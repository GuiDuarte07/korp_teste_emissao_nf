using InvoiceService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Invoice;

namespace InvoiceService.Consumers;

public class UpdateInvoiceSnapshotConsumer : IConsumer<UpdateInvoiceSnapshotRequest>
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<UpdateInvoiceSnapshotConsumer> _logger;

    public UpdateInvoiceSnapshotConsumer(InvoiceDbContext context, ILogger<UpdateInvoiceSnapshotConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateInvoiceSnapshotRequest> context)
    {
        try
        {
            var request = context.Message;
            
            _logger.LogInformation("Atualizando snapshot da nota fiscal {InvoiceId} com reserva {ReservationId}", 
                request.InvoiceId, request.ReservationId);

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId);

            if (invoice == null)
            {
                _logger.LogWarning("Nota fiscal {InvoiceId} não encontrada para atualizar snapshot", request.InvoiceId);
                return;
            }

            // Atualiza cada item com o snapshot do produto e ReservationId
            foreach (var item in invoice.Items)
            {
                var snapshotData = request.Items.FirstOrDefault(s => s.ProductId == item.ProductId);
                
                if (snapshotData != null)
                {
                    item.ProductCode = snapshotData.ProductCode;
                    item.ProductDescription = snapshotData.ProductDescription;
                    item.ReservationId = request.ReservationId;
                    
                    _logger.LogInformation("Item {ItemId} atualizado: {ProductCode} - {ProductDescription}", 
                        item.Id, item.ProductCode, item.ProductDescription);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Snapshot da nota fiscal #{InvoiceNumber} atualizado com sucesso", invoice.InvoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar snapshot da nota fiscal {InvoiceId}", context.Message.InvoiceId);
            // Fire-and-forget: não envia resposta, apenas loga o erro
        }
    }
}
