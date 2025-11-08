using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Infrastructure.Data;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace InventoryService.Consumers;

public class UpdateProductConsumer : IConsumer<UpdateProductRequest>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<UpdateProductConsumer> _logger;

    public UpdateProductConsumer(InventoryDbContext context, ILogger<UpdateProductConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateProductRequest> context)
    {
        try
        {
            var request = context.Message;
            var product = await _context.Products.FindAsync(request.Id);

            if (product == null)
            {
                await context.RespondAsync(Result<ProductDto>.Failure(
                    ErrorCode.PRODUCT_NOT_FOUND,
                    $"Produto com ID {request.Id} não encontrado"
                ));
                return;
            }

            if (!string.IsNullOrEmpty(request.Code))
            {
                product.Code = request.Code;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                product.Description = request.Description;
            }

            // Calcula o estoque reservado atual do produto (usado para validação e resposta)
            var reservedStock = await _context.StockReservationItems
                .Where(ri => ri.ProductId == product.Id 
                    && !ri.Reservation.Confirmed 
                    && !ri.Reservation.Cancelled)
                .SumAsync(ri => ri.Quantity);

            if (request.Stock.HasValue)
            {
                // Valida se o novo estoque não é menor que o estoque reservado
                if (request.Stock.Value < reservedStock)
                {
                    await context.RespondAsync(Result<ProductDto>.Failure(
                        ErrorCode.VALIDATION_ERROR,
                        $"Não é permitido definir o estoque ({request.Stock.Value}) para um valor menor que o estoque reservado ({reservedStock})"
                    ));
                    return;
                }

                product.Stock = request.Stock.Value;
            }

            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Produto atualizado: {ProductId}", product.Id);

            var response = new ProductDto
            {
                Id = product.Id,
                Code = product.Code,
                Description = product.Description,
                Stock = product.Stock,
                ReservedStock = reservedStock
            };

            await context.RespondAsync(Result<ProductDto>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produto {ProductId}", context.Message.Id);
            await context.RespondAsync(Result<ProductDto>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao atualizar produto"
            ));
        }
    }
}
