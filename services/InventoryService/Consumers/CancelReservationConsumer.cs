using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Infrastructure.Data;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace InventoryService.Consumers;

public class CancelReservationConsumer : IConsumer<CancelReservationRequest>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<CancelReservationConsumer> _logger;

    public CancelReservationConsumer(InventoryDbContext context, ILogger<CancelReservationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CancelReservationRequest> context)
    {
        try
        {
            var reservation = await _context.StockReservations
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(r => r.InvoiceId == context.Message.InvoiceId);

            if (reservation == null)
            {
                _logger.LogWarning("Reserva para Invoice {InvoiceId} não encontrada", context.Message.InvoiceId);
                await context.RespondAsync(Result<StockReservationResponse>.Failure(
                    ErrorCode.RESERVATION_NOT_FOUND,
                    $"Reserva para Invoice {context.Message.InvoiceId} não encontrada"
                ));
                return;
            }

            if (reservation.Confirmed)
            {
                _logger.LogWarning("Tentativa de cancelar reserva {ReservationId} já confirmada para Invoice {InvoiceId}", 
                    reservation.Id, context.Message.InvoiceId);
                await context.RespondAsync(Result<StockReservationResponse>.Failure(
                    ErrorCode.ALREADY_CONFIRMED,
                    "Não é possível cancelar uma reserva já confirmada"
                ));
                return;
            }

            if (reservation.Cancelled)
            {
                _logger.LogInformation("Reserva {ReservationId} para Invoice {InvoiceId} já estava cancelada", 
                    reservation.Id, context.Message.InvoiceId);
                
                var alreadyCancelledResponse = BuildResponse(reservation);
                await context.RespondAsync(Result<StockReservationResponse>.Success(alreadyCancelledResponse));
                return;
            }

            reservation.Cancelled = true;
            reservation.CancelledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Reserva {ReservationId} para Invoice {InvoiceId} cancelada com sucesso", 
                reservation.Id, context.Message.InvoiceId);

            var response = BuildResponse(reservation);
            await context.RespondAsync(Result<StockReservationResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar reserva para Invoice {InvoiceId}", context.Message.InvoiceId);
            await context.RespondAsync(Result<StockReservationResponse>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao cancelar reserva"
            ));
        }
    }

    private StockReservationResponse BuildResponse(Domain.Entities.StockReservation reservation)
    {
        return new StockReservationResponse
        {
            Id = reservation.Id,
            InvoiceId = reservation.InvoiceId,
            Status = reservation.Confirmed ? "Confirmada" : reservation.Cancelled ? "Cancelada" : "Pendente",
            CreatedAt = reservation.CreatedAt,
            Items = reservation.Items.Select(i => new StockReservationItemResponse
            {
                ProductId = i.ProductId,
                ProductCode = i.Product.Code,
                ProductDescription = i.Product.Description,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
