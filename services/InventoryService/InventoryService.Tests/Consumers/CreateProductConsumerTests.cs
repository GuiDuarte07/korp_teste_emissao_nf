using FluentAssertions;
using InventoryService.Consumers;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Common;
using Shared.Contracts.Inventory;
using Xunit;

namespace InventoryService.Tests.Consumers;

public class CreateProductConsumerTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly CreateProductConsumer _consumer;
    private readonly Mock<ILogger<CreateProductConsumer>> _loggerMock;

    public CreateProductConsumerTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new InventoryDbContext(options);
        _loggerMock = new Mock<ILogger<CreateProductConsumer>>();
        _consumer = new CreateProductConsumer(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldCreateProduct_WhenValidRequest()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Code = "PROD001",
            Description = "Produto Teste",
            InitialStock = 100
        };

        var contextMock = new Mock<ConsumeContext<CreateProductRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<CreateProductResponse>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        var product = await _context.Products.FirstOrDefaultAsync();
        product.Should().NotBeNull();
        product!.Code.Should().Be("PROD001");
        product.Description.Should().Be("Produto Teste");
        product.Stock.Should().Be(100);
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        contextMock.Verify(c => c.RespondAsync(It.Is<Result<CreateProductResponse>>(
            r => r.IsSuccess && r.Data!.Code == "PROD001"
        )), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldReturnError_WhenDuplicateCode()
    {
        // Arrange - Criar produto existente
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Code = "PROD001",
            Description = "Produto Existente",
            Stock = 50,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(existingProduct);
        await _context.SaveChangesAsync();

        var request = new CreateProductRequest
        {
            Code = "PROD001", // Mesmo código
            Description = "Produto Novo",
            InitialStock = 100
        };

        var contextMock = new Mock<ConsumeContext<CreateProductRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<CreateProductResponse>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert - Não deve criar novo produto
        var products = await _context.Products.ToListAsync();
        products.Should().HaveCount(1);
        products.First().Description.Should().Be("Produto Existente"); // Mantém o original

        contextMock.Verify(c => c.RespondAsync(It.Is<Result<CreateProductResponse>>(
            r => !r.IsSuccess && r.ErrorCode == ErrorCode.DUPLICATE_CODE
        )), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldCreateMultipleProducts_WithDifferentCodes()
    {
        // Arrange
        var request1 = new CreateProductRequest
        {
            Code = "PROD001",
            Description = "Produto 1",
            InitialStock = 10
        };

        var request2 = new CreateProductRequest
        {
            Code = "PROD002",
            Description = "Produto 2",
            InitialStock = 20
        };

        var contextMock1 = new Mock<ConsumeContext<CreateProductRequest>>();
        contextMock1.Setup(c => c.Message).Returns(request1);
        contextMock1.Setup(c => c.RespondAsync(It.IsAny<Result<CreateProductResponse>>()))
            .Returns(Task.CompletedTask);

        var contextMock2 = new Mock<ConsumeContext<CreateProductRequest>>();
        contextMock2.Setup(c => c.Message).Returns(request2);
        contextMock2.Setup(c => c.RespondAsync(It.IsAny<Result<CreateProductResponse>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock1.Object);
        await _consumer.Consume(contextMock2.Object);

        // Assert
        var products = await _context.Products.OrderBy(p => p.Code).ToListAsync();
        products.Should().HaveCount(2);
        products[0].Code.Should().Be("PROD001");
        products[0].Stock.Should().Be(10);
        products[1].Code.Should().Be("PROD002");
        products[1].Stock.Should().Be(20);
    }

    [Fact]
    public async Task Consume_ShouldCreateProduct_WithZeroInitialStock()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Code = "PROD999",
            Description = "Produto Sem Estoque",
            InitialStock = 0
        };

        var contextMock = new Mock<ConsumeContext<CreateProductRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<CreateProductResponse>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        var product = await _context.Products.FirstOrDefaultAsync();
        product.Should().NotBeNull();
        product!.Stock.Should().Be(0);

        contextMock.Verify(c => c.RespondAsync(It.Is<Result<CreateProductResponse>>(
            r => r.IsSuccess
        )), Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
