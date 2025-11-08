using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Infrastructure.Data;
using InventoryService.Domain.Entities;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace InventoryService.Consumers;

public class CreateProductConsumer : IConsumer<CreateProductRequest>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<CreateProductConsumer> _logger;

    public CreateProductConsumer(InventoryDbContext context, ILogger<CreateProductConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateProductRequest> context)
    {
        try
        {
            var request = context.Message;

            // Verifica duplicidade de código
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Code == request.Code);

            if (existingProduct != null)
            {
                await context.RespondAsync(Result<CreateProductResponse>.Failure(
                    ErrorCode.DUPLICATE_CODE,
                    $"Produto com código '{request.Code}' já existe"
                ));
                return;
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Description = request.Description,
                Stock = request.InitialStock,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Produto criado: {ProductId} - {ProductCode}", product.Id, product.Code);

            var response = new CreateProductResponse
            {
                Id = product.Id,
                Code = product.Code,
                Description = product.Description,
                Stock = product.Stock
            };

            await context.RespondAsync(Result<CreateProductResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar produto {ProductCode}", context.Message.Code);
            await context.RespondAsync(Result<CreateProductResponse>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao criar produto"
            ));
        }
    }
}
