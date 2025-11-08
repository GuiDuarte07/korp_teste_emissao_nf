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
            _logger.LogInformation("Deletando nota fiscal {InvoiceId}", context.Message.Id);

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

            // Nota: apenas notas Open podem ser deletadas (regra de negócio)
            // Notas Closed já foram impressas e estoque debitado
            if (invoice.Status == Domain.Entities.InvoiceStatus.Closed)
            {
                _logger.LogWarning("Tentativa de deletar nota fiscal fechada {InvoiceNumber}", invoice.InvoiceNumber);
                await context.RespondAsync(Result.Failure(
                    ErrorCode.INVALID_REQUEST,
                    "Não é possível deletar nota fiscal já impressa (fechada)"));
                return;
            }

            // Cancela a reserva de estoque antes de deletar a nota
            // Usa Publish (fire-and-forget) ao invés de Request/Response porque:
            // 1. Não precisamos esperar pela resposta (nota será deletada de qualquer forma)
            // 2. RabbitMQ garante entrega: mensagem fica na fila até InventoryService processar
            // 3. Resiliente: funciona mesmo se InventoryService estiver temporariamente fora

            _logger.LogInformation("Publicando evento de cancelamento de reserva da nota #{InvoiceNumber}", invoice.InvoiceNumber);
            
            await context.Publish(new CancelReservationRequest 
            { 
                InvoiceId = invoice.Id 
            });

            // Delete cascade vai remover os InvoiceItems automaticamente
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nota fiscal #{InvoiceNumber} deletada com sucesso. Cancelamentos de reserva publicados.", invoice.InvoiceNumber);

            await context.RespondAsync(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar nota fiscal {InvoiceId}", context.Message.Id);
            await context.RespondAsync(Result.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao deletar nota fiscal"));
        }
    }
}
