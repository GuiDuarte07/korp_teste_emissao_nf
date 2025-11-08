using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Infrastructure.Data;
using InventoryService.Domain.Entities;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace InventoryService.Consumers;

public class CreateReservationConsumer : IConsumer<CreateStockReservationRequest>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<CreateReservationConsumer> _logger;

    public CreateReservationConsumer(InventoryDbContext context, ILogger<CreateReservationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateStockReservationRequest> context)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var request = context.Message;

            // Verifica disponibilidade de estoque para todos os produtos
            var productIds = request.Items.Select(i => i.ProductId).ToList();
            
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var insufficientStockProducts = new List<string>();

            foreach (var item in request.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                
                if (product == null)
                {
                    await context.RespondAsync(Result<StockReservationResponse>.Failure(
                        ErrorCode.PRODUCT_NOT_FOUND,
                        $"Produto com ID {item.ProductId} não encontrado"
                    ));
                    return;
                }

                // Calcula estoque reservado
                var reservedStock = await _context.StockReservationItems
                    .Where(ri => ri.ProductId == product.Id 
                        && !ri.Reservation.Confirmed 
                        && !ri.Reservation.Cancelled)
                    .SumAsync(ri => ri.Quantity);

                var availableStock = product.Stock - reservedStock;

                if (availableStock < item.Quantity)
                {
                    insufficientStockProducts.Add($"{product.Code} (disponível: {availableStock}, solicitado: {item.Quantity})");
                }
            }

            if (insufficientStockProducts.Any())
            {
                await context.RespondAsync(Result<StockReservationResponse>.Failure(
                    ErrorCode.INSUFFICIENT_STOCK,
                    $"Estoque insuficiente para: {string.Join(", ", insufficientStockProducts)}"
                ));
                return;
            }

            // Cria a reserva
            var reservation = new StockReservation
            {
                Id = Guid.NewGuid(),
                InvoiceId = request.InvoiceId,
                CreatedAt = DateTime.UtcNow,
                Confirmed = false,
                Cancelled = false
            };

            _context.StockReservations.Add(reservation);

            // Cria os itens da reserva
            foreach (var item in request.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                
                var reservationItem = new StockReservationItem
                {
                    Id = Guid.NewGuid(),
                    ReservationId = reservation.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                };

                _context.StockReservationItems.Add(reservationItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Reserva criada: {ReservationId} para nota fiscal {InvoiceId}", 
                reservation.Id, reservation.InvoiceId);

            // Monta a resposta
            var response = new StockReservationResponse
            {
                Id = reservation.Id,
                InvoiceId = reservation.InvoiceId,
                Status = "Pendente",
                CreatedAt = reservation.CreatedAt,
                Items = request.Items.Select(i =>
                {
                    var product = products.First(p => p.Id == i.ProductId);
                    return new StockReservationItemResponse
                    {
                        ProductId = product.Id,
                        ProductCode = product.Code,
                        ProductDescription = product.Description,
                        Quantity = i.Quantity
                    };
                }).ToList()
            };

            await context.RespondAsync(Result<StockReservationResponse>.Success(response));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Erro ao criar reserva para nota fiscal {InvoiceId}", context.Message.InvoiceId);
            await context.RespondAsync(Result<StockReservationResponse>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao criar reserva"
            ));
        }
    }
}
