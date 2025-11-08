using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Infrastructure.Data;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace InventoryService.Consumers;

public class ConfirmReservationConsumer : IConsumer<ConfirmReservationRequest>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<ConfirmReservationConsumer> _logger;

    public ConfirmReservationConsumer(InventoryDbContext context, ILogger<ConfirmReservationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConfirmReservationRequest> context)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
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

            // Se já foi confirmada, retorna sucesso
            if (reservation.Confirmed)
            {
                _logger.LogInformation("Reserva {ReservationId} já estava confirmada (idempotência)", reservation.Id);
                
                var alreadyConfirmedResponse = BuildResponse(reservation);
                await context.RespondAsync(Result<StockReservationResponse>.Success(alreadyConfirmedResponse));
                return;
            }

            if (reservation.Cancelled)
            {
                await context.RespondAsync(Result<StockReservationResponse>.Failure(
                    ErrorCode.ALREADY_CANCELLED,
                    "Não é possível confirmar uma reserva cancelada"
                ));
                return;
            }

            // Debita o estoque de cada produto
            foreach (var item in reservation.Items)
            {
                item.Product.Stock -= item.Quantity;
                
                if (item.Product.Stock < 0)
                {
                    await transaction.RollbackAsync();
                    await context.RespondAsync(Result<StockReservationResponse>.Failure(
                        ErrorCode.INSUFFICIENT_STOCK,
                        $"Estoque insuficiente para produto {item.Product.Code}"
                    ));
                    return;
                }
            }

            reservation.Confirmed = true;
            reservation.ConfirmedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Reserva confirmada: {ReservationId}", reservation.Id);

            var response = BuildResponse(reservation);
            await context.RespondAsync(Result<StockReservationResponse>.Success(response));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao confirmar reserva {ReservationId}", context.Message.ReservationId);
            await context.RespondAsync(Result<StockReservationResponse>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao confirmar reserva"
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
