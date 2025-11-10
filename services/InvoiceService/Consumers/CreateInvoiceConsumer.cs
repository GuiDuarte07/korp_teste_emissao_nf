using InvoiceService.Domain.Entities;
using InvoiceService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Contracts.Invoice;

namespace InvoiceService.Consumers;

public class CreateInvoiceConsumer : IConsumer<CreateInvoiceRequest>
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<CreateInvoiceConsumer> _logger;

    public CreateInvoiceConsumer(InvoiceDbContext context, ILogger<CreateInvoiceConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateInvoiceRequest> context)
    {
        // Usar transação com Serializable para prevenir race conditions
        using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable
        );

        try
        {
            var request = context.Message;
            _logger.LogInformation("Criando nova nota fiscal com {ItemCount} itens", request.Items.Count);

            // ===== VERIFICAR IDEMPOTÊNCIA =====
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                // Buscar chave de idempotência existente COM LOCK
                var existingKey = await _context.IdempotencyKeys
                    .FirstOrDefaultAsync(k => k.Key == request.IdempotencyKey);

                if (existingKey != null)
                {
                    // Requisição duplicada detectada!
                    _logger.LogInformation(
                        "Idempotency key duplicada: {Key}. Retornando invoice existente: {InvoiceId}",
                        request.IdempotencyKey,
                        existingKey.InvoiceId
                    );

                    // Buscar invoice existente para retornar
                    var existingInvoice = await _context.Invoices
                        .Include(i => i.Items)
                        .FirstOrDefaultAsync(i => i.Id == existingKey.InvoiceId);

                    if (existingInvoice != null)
                    {
                        var existingDto = MapToDto(existingInvoice);
                        await context.RespondAsync(Result<InvoiceDto>.Success(existingDto));
                        await transaction.CommitAsync();
                        return;
                    }
                }

                // ===== REGISTRAR KEY PRIMEIRO (previne race condition) =====
                // Se chegou aqui, key não existe. Vamos registrá-la ANTES de criar a invoice
                var tempInvoiceId = Guid.NewGuid();
                var idempotencyKey = new IdempotencyKey
                {
                    Id = Guid.NewGuid(),
                    Key = request.IdempotencyKey,
                    InvoiceId = tempInvoiceId, // ID temporário
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    ResponsePayload = null
                };

                _context.IdempotencyKeys.Add(idempotencyKey);
                
                try
                {
                    // Tentar salvar a key primeiro (pode falhar se outra thread já salvou)
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation(
                        "Idempotency key registrada: {Key} -> Invoice {InvoiceId}",
                        request.IdempotencyKey,
                        tempInvoiceId
                    );
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
                {
                    // Outra thread registrou a key primeiro - buscar invoice dela
                    _logger.LogInformation("Key já foi registrada por outra thread, buscando invoice existente");
                    
                    await transaction.RollbackAsync();
                    
                    // Aguardar um pouco para dar tempo da outra thread criar a invoice
                    await Task.Delay(100);
                    
                    var otherKey = await _context.IdempotencyKeys
                        .AsNoTracking()
                        .FirstOrDefaultAsync(k => k.Key == request.IdempotencyKey);

                    if (otherKey != null)
                    {
                        var otherInvoice = await _context.Invoices
                            .AsNoTracking()
                            .Include(i => i.Items)
                            .FirstOrDefaultAsync(i => i.Id == otherKey.InvoiceId);

                        if (otherInvoice != null)
                        {
                            var dto = MapToDto(otherInvoice);
                            await context.RespondAsync(Result<InvoiceDto>.Success(dto));
                            return;
                        }
                    }
                    
                    // Se não encontrou, retornar erro
                    await context.RespondAsync(Result<InvoiceDto>.Failure(
                        ErrorCode.DUPLICATE_REQUEST,
                        "Requisição duplicada detectada, mas invoice ainda não foi criada"));
                    return;
                }
            }

            // ===== CRIAR NOVA NOTA FISCAL =====
            // Gera próximo número sequencial
            var lastInvoiceNumber = await _context.Invoices
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            var newInvoiceNumber = lastInvoiceNumber + 1;

            // Cria a nota fiscal
            var invoice = new Invoice
            {
                Id = request.IdempotencyKey != null 
                    ? await GetInvoiceIdFromKey(request.IdempotencyKey)
                    : Guid.NewGuid(),
                InvoiceNumber = newInvoiceNumber,
                Status = InvoiceStatus.Open,
                CreatedAt = DateTime.UtcNow,
                Items = new List<InvoiceItem>()
            };

            // Adiciona os itens (snapshot será preenchido pelo Saga no ApiGateway)
            foreach (var item in request.Items)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductId = item.ProductId,
                    ProductCode = string.Empty,
                    ProductDescription = string.Empty,
                    Quantity = item.Quantity,
                    ReservationId = null
                });
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Nota fiscal #{InvoiceNumber} criada com sucesso", newInvoiceNumber);

            var invoiceDto = MapToDto(invoice);
            await context.RespondAsync(Result<InvoiceDto>.Success(invoiceDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar nota fiscal");
            await transaction.RollbackAsync();
            
            await context.RespondAsync(Result<InvoiceDto>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao criar nota fiscal"));
        }
    }

    private async Task<Guid> GetInvoiceIdFromKey(string idempotencyKey)
    {
        var key = await _context.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Key == idempotencyKey);
        
        return key?.InvoiceId ?? Guid.NewGuid();
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Status = invoice.Status == InvoiceStatus.Open ? "Open" : "Closed",
            CreatedAt = invoice.CreatedAt,
            Cancelled = invoice.Cancelled,
            CancelledAt = invoice.CancelledAt,
            Items = invoice.Items.Select(i => new InvoiceItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductCode = i.ProductCode,
                ProductDescription = i.ProductDescription,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
