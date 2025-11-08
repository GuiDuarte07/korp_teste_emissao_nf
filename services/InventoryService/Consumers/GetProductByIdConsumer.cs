using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Infrastructure.Data;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace InventoryService.Consumers;

public class GetProductByIdConsumer : IConsumer<GetProductByIdRequest>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<GetProductByIdConsumer> _logger;

    public GetProductByIdConsumer(InventoryDbContext context, ILogger<GetProductByIdConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetProductByIdRequest> context)
    {
        try
        {
            var product = await _context.Products
                .Where(p => p.Id == context.Message.Id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Description = p.Description,
                    Stock = p.Stock,
                    ReservedStock = p.ReservationItems
                        .Where(ri => !ri.Reservation.Confirmed && !ri.Reservation.Cancelled)
                        .Sum(ri => ri.Quantity)
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                await context.RespondAsync(Result<ProductDto>.Failure(
                    ErrorCode.PRODUCT_NOT_FOUND,
                    $"Produto com ID {context.Message.Id} não encontrado"
                ));
                return;
            }

            await context.RespondAsync(Result<ProductDto>.Success(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produto {ProductId}", context.Message.Id);
            await context.RespondAsync(Result<ProductDto>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao buscar produto"
            ));
        }
    }
}
