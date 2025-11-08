using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Infrastructure.Data;
using InventoryService.Domain.Entities;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace InventoryService.Consumers;

public class GetAllProductsConsumer : IConsumer<GetAllProductsRequest>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<GetAllProductsConsumer> _logger;

    public GetAllProductsConsumer(InventoryDbContext context, ILogger<GetAllProductsConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetAllProductsRequest> context)
    {
        _logger.LogInformation("=== RECEBEU MENSAGEM GetAllProductsRequest ===");
        
        try
        {
            var products = await _context.Products
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
                .ToListAsync();

            _logger.LogInformation("=== ENVIANDO RESPOSTA: {Count} produtos ===", products.Count);
            
            var result = Result<List<ProductDto>>.Success(products);
            await context.RespondAsync(result);
            
            _logger.LogInformation("=== RESPOSTA ENVIADA COM SUCESSO ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produtos");
            await context.RespondAsync(Result<List<ProductDto>>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao buscar produtos"
            ));
        }
    }
}
