using InventoryService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Invoice;

namespace InventoryService.Consumers;

/// <summary>
/// Consumer que reage ao evento de cancelamento de invoice.
/// Cancela a reserva de estoque de forma assíncrona e desacoplada do InvoiceService.
/// </summary>
public class InvoiceCancelledConsumer : IConsumer<InvoiceCancelledEvent>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<InvoiceCancelledConsumer> _logger;

    public InvoiceCancelledConsumer(InventoryDbContext context, ILogger<InvoiceCancelledConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvoiceCancelledEvent> context)
    {
        try
        {
            var evt = context.Message;
            
            _logger.LogInformation("Recebido evento de cancelamento da invoice #{InvoiceNumber} (InvoiceId: {InvoiceId})", 
                evt.InvoiceNumber, evt.InvoiceId);

            // Busca a reserva pelo InvoiceId
            var reservation = await _context.StockReservations
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.InvoiceId == evt.InvoiceId);

            if (reservation == null)
            {
                _logger.LogWarning("Reserva para Invoice {InvoiceId} não encontrada. Possivelmente já foi cancelada ou nunca existiu.", 
                    evt.InvoiceId);
                return;
            }

            // Idempotência: se já cancelada, ignora
            if (reservation.Cancelled)
            {
                _logger.LogInformation("Reserva {ReservationId} para Invoice {InvoiceId} já estava cancelada. Ignorando evento duplicado.", 
                    reservation.Id, evt.InvoiceId);
                return;
            }

            // Verifica se já foi confirmada (estoque já debitado)
            if (reservation.Confirmed)
            {
                _logger.LogWarning("Tentativa de cancelar reserva {ReservationId} já confirmada para Invoice {InvoiceId}. Operação não permitida.", 
                    reservation.Id, evt.InvoiceId);
                return;
            }

            // Cancela a reserva
            reservation.Cancelled = true;
            reservation.CancelledAt = evt.CancelledAt;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Reserva {ReservationId} para Invoice #{InvoiceNumber} cancelada com sucesso em resposta ao evento de domínio", 
                reservation.Id, evt.InvoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar evento de cancelamento de invoice {InvoiceId}", 
                context.Message.InvoiceId);
        }
    }
}
