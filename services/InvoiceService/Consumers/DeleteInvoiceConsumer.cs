using InvoiceService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Contracts.Inventory;
using Shared.Contracts.Invoice;

namespace InvoiceService.Consumers;

public class DeleteInvoiceConsumer : IConsumer<DeleteInvoiceRequest>
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<DeleteInvoiceConsumer> _logger;

    public DeleteInvoiceConsumer(
        InvoiceDbContext context,
        ILogger<DeleteInvoiceConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteInvoiceRequest> context)
    {
        try
        {
            _logger.LogInformation("Cancelando nota fiscal {InvoiceId}", context.Message.Id);

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == context.Message.Id);

            if (invoice == null)
            {
                _logger.LogWarning("Nota fiscal {InvoiceId} não encontrada", context.Message.Id);
                await context.RespondAsync(Result.Failure(
                    ErrorCode.NOT_FOUND,
                    $"Nota fiscal {context.Message.Id} não encontrada"));
                return;
            }

            if (invoice.Cancelled)
            {
                _logger.LogWarning("Nota fiscal #{InvoiceNumber} já está cancelada", invoice.InvoiceNumber);
                await context.RespondAsync(Result.Failure(
                    ErrorCode.INVALID_REQUEST,
                    "Nota fiscal já está cancelada"));
                return;
            }

            // Nota: apenas notas Open podem ser canceladas (regra de negócio)
            // Notas Closed já foram impressas e estoque debitado
            if (invoice.Status == Domain.Entities.InvoiceStatus.Closed)
            {
                _logger.LogWarning("Tentativa de cancelar nota fiscal fechada {InvoiceNumber}", invoice.InvoiceNumber);
                await context.RespondAsync(Result.Failure(
                    ErrorCode.INVALID_REQUEST,
                    "Não é possível cancelar nota fiscal já impressa (fechada)"));
                return;
            }

            // Marca como cancelada
            invoice.Cancelled = true;
            invoice.CancelledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Nota fiscal #{InvoiceNumber} cancelada com sucesso", invoice.InvoiceNumber);

            // Publica evento de domínio para que outros serviços reajam ao cancelamento
            // InventoryService irá assinar este evento e cancelar a reserva de forma assíncrona e desacoplada
            _logger.LogInformation("Publicando evento InvoiceCancelledEvent para nota #{InvoiceNumber}", invoice.InvoiceNumber);
            
            await context.Publish(new InvoiceCancelledEvent
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CancelledAt = invoice.CancelledAt.Value,
                Items = invoice.Items.Select(i => new InvoiceCancelledItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            });

            await context.RespondAsync(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar nota fiscal {InvoiceId}", context.Message.Id);
            await context.RespondAsync(Result.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao cancelar nota fiscal"));
        }
    }
}
