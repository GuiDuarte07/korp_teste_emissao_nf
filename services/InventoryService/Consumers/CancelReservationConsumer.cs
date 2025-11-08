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
                .FirstOrDefaultAsync(r => r.Id == context.Message.ReservationId);

            if (reservation == null)
            {
                await context.RespondAsync(Result<StockReservationResponse>.Failure(
                    ErrorCode.RESERVATION_NOT_FOUND,
                    $"Reserva com ID {context.Message.ReservationId} não encontrada"
                ));
                return;
            }

            if (reservation.Confirmed)
            {
                await context.RespondAsync(Result<StockReservationResponse>.Failure(
                    ErrorCode.ALREADY_CONFIRMED,
                    "Não é possível cancelar uma reserva já confirmada"
                ));
                return;
            }

            if (reservation.Cancelled)
            {
                _logger.LogInformation("Reserva {ReservationId} já estava cancelada (idempotência)", reservation.Id);
                
                var alreadyCancelledResponse = BuildResponse(reservation);
                await context.RespondAsync(Result<StockReservationResponse>.Success(alreadyCancelledResponse));
                return;
            }

            reservation.Cancelled = true;
            reservation.CancelledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Reserva cancelada: {ReservationId}", reservation.Id);

            var response = BuildResponse(reservation);
            await context.RespondAsync(Result<StockReservationResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar reserva {ReservationId}", context.Message.ReservationId);
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
