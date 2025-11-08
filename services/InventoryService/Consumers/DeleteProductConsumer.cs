using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Infrastructure.Data;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace InventoryService.Consumers;

public class DeleteProductConsumer : IConsumer<DeleteProductRequest>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<DeleteProductConsumer> _logger;

    public DeleteProductConsumer(InventoryDbContext context, ILogger<DeleteProductConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeleteProductRequest> context)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.ReservationItems)
                .FirstOrDefaultAsync(p => p.Id == context.Message.Id);

            if (product == null)
            {
                await context.RespondAsync(Result.Failure(
                    ErrorCode.PRODUCT_NOT_FOUND,
                    $"Produto com ID {context.Message.Id} não encontrado"
                ));
                return;
            }

            // Verifica se há reservas associadas
            if (product.ReservationItems.Any())
            {
                await context.RespondAsync(Result.Failure(
                    ErrorCode.HAS_RESERVATIONS,
                    "Não é possível deletar produto com reservas associadas"
                ));
                return;
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Produto deletado: {ProductId}", product.Id);

            await context.RespondAsync(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar produto {ProductId}", context.Message.Id);
            await context.RespondAsync(Result.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao deletar produto"
            ));
        }
    }
}
