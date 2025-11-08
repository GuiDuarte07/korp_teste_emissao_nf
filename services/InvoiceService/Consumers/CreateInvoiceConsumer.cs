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
        try
        {
            _logger.LogInformation("Criando nova nota fiscal com {ItemCount} itens", context.Message.Items.Count);

            // Gera próximo número sequencial
            var lastInvoiceNumber = await _context.Invoices
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            var newInvoiceNumber = lastInvoiceNumber + 1;

            // Cria a nota fiscal
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = newInvoiceNumber,
                Status = InvoiceStatus.Open,
                CreatedAt = DateTime.UtcNow,
                Items = new List<InvoiceItem>()
            };

            // Adiciona os itens (snapshot será preenchido pelo Saga no ApiGateway)
            foreach (var item in context.Message.Items)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductId = item.ProductId,
                    ProductCode = string.Empty, // Será preenchido com snapshot
                    ProductDescription = string.Empty, // Será preenchido com snapshot
                    Quantity = item.Quantity,
                    ReservationId = null // Será preenchido quando reserva for criada
                });
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nota fiscal #{InvoiceNumber} criada com sucesso", newInvoiceNumber);

            var invoiceDto = new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Status = "Open",
                CreatedAt = invoice.CreatedAt,
                Items = invoice.Items.Select(i => new InvoiceItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductCode = i.ProductCode,
                    ProductDescription = i.ProductDescription,
                    Quantity = i.Quantity
                }).ToList()
            };

            await context.RespondAsync(Result<InvoiceDto>.Success(invoiceDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar nota fiscal");
            await context.RespondAsync(Result<InvoiceDto>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao criar nota fiscal"));
        }
    }
}
