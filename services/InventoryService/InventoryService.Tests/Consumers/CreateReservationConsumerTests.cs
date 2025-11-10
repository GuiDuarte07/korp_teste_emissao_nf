using FluentAssertions;
using InventoryService.Consumers;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Data;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Common;
using Shared.Contracts.Inventory;
using Xunit;

namespace InventoryService.Tests.Consumers;

public class CreateReservationConsumerTests : IDisposable
{
    private readonly InventoryDbContext _context;
    private readonly CreateReservationConsumer _consumer;
    private readonly Mock<ILogger<CreateReservationConsumer>> _loggerMock;
    private readonly SqliteConnection _connection;

    public CreateReservationConsumerTests()
    {
        // SQLite InMemory database - suporta transações e queries SQL
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new InventoryDbContext(options);
        _context.Database.EnsureCreated();
        
        _loggerMock = new Mock<ILogger<CreateReservationConsumer>>();
        _consumer = new CreateReservationConsumer(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldCreateReservation_WhenStockAvailable()
    {
        // Arrange - Criar produto com estoque
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "PROD001",
            Description = "Produto Teste",
            Stock = 100,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var invoiceId = Guid.NewGuid();
        var request = new CreateStockReservationRequest
        {
            InvoiceId = invoiceId,
            Items = new List<CreateStockReservationItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 10 }
            }
        };

        Result<StockReservationResponse>? capturedResponse = null;
        var contextMock = new Mock<ConsumeContext<CreateStockReservationRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<StockReservationResponse>>()))
            .Callback<Result<StockReservationResponse>>(r => capturedResponse = r)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse!.IsSuccess.Should().BeTrue();
        
        var reservation = await _context.StockReservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync();

        reservation.Should().NotBeNull();
        reservation!.InvoiceId.Should().Be(invoiceId);
        reservation.Confirmed.Should().BeFalse();
        reservation.Cancelled.Should().BeFalse();
        reservation.Items.Should().HaveCount(1);
        reservation.Items.First().Quantity.Should().Be(10);
    }

    [Fact]
    public async Task Consume_ShouldReturnError_WhenInsufficientStock()
    {
        // Arrange - Criar produto com estoque insuficiente
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "PROD001",
            Description = "Produto Teste",
            Stock = 5, // Estoque baixo
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var request = new CreateStockReservationRequest
        {
            InvoiceId = Guid.NewGuid(),
            Items = new List<CreateStockReservationItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 10 } // Solicita mais que disponível
            }
        };

        var contextMock = new Mock<ConsumeContext<CreateStockReservationRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        
        Result<StockReservationResponse>? capturedResponse = null;
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<StockReservationResponse>>()))
            .Callback<Result<StockReservationResponse>>(r => capturedResponse = r)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse!.IsSuccess.Should().BeFalse();
        capturedResponse.ErrorCode.Should().Be(ErrorCode.INSUFFICIENT_STOCK);
        
        var reservations = await _context.StockReservations.ToListAsync();
        reservations.Should().BeEmpty(); // Não deve criar reserva
    }

    [Fact]
    public async Task Consume_ShouldReturnError_WhenProductNotFound()
    {
        // Arrange - Não criar nenhum produto
        var nonExistentProductId = Guid.NewGuid();
        var request = new CreateStockReservationRequest
        {
            InvoiceId = Guid.NewGuid(),
            Items = new List<CreateStockReservationItemRequest>
            {
                new() { ProductId = nonExistentProductId, Quantity = 10 }
            }
        };

        var contextMock = new Mock<ConsumeContext<CreateStockReservationRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        
        Result<StockReservationResponse>? capturedResponse = null;
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<StockReservationResponse>>()))
            .Callback<Result<StockReservationResponse>>(r => capturedResponse = r)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse!.IsSuccess.Should().BeFalse();
        capturedResponse.ErrorCode.Should().Be(ErrorCode.PRODUCT_NOT_FOUND);
    }

    [Fact]
    public async Task Consume_ShouldCreateReservation_WithMultipleItems()
    {
        // Arrange - Criar múltiplos produtos
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Code = "PROD001",
            Description = "Produto 1",
            Stock = 50,
            CreatedAt = DateTime.UtcNow
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Code = "PROD002",
            Description = "Produto 2",
            Stock = 30,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        var request = new CreateStockReservationRequest
        {
            InvoiceId = Guid.NewGuid(),
            Items = new List<CreateStockReservationItemRequest>
            {
                new() { ProductId = product1.Id, Quantity = 10 },
                new() { ProductId = product2.Id, Quantity = 5 }
            }
        };

        var contextMock = new Mock<ConsumeContext<CreateStockReservationRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        
        Result<StockReservationResponse>? capturedResponse = null;
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<StockReservationResponse>>()))
            .Callback<Result<StockReservationResponse>>(r => capturedResponse = r)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse!.IsSuccess.Should().BeTrue();
        
        var reservation = await _context.StockReservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync();

        reservation.Should().NotBeNull();
        reservation!.Items.Should().HaveCount(2);
        reservation.Items.Should().Contain(i => i.ProductId == product1.Id && i.Quantity == 10);
        reservation.Items.Should().Contain(i => i.ProductId == product2.Id && i.Quantity == 5);
    }

    [Fact]
    public async Task Consume_ShouldConsiderExistingReservations_WhenCalculatingAvailableStock()
    {
        // Arrange - Criar produto
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "PROD001",
            Description = "Produto Teste",
            Stock = 20,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);

        // Criar reserva existente de 15 unidades
        var existingReservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Confirmed = false,
            Cancelled = false
        };
        _context.StockReservations.Add(existingReservation);

        var existingItem = new StockReservationItem
        {
            Id = Guid.NewGuid(),
            ReservationId = existingReservation.Id,
            ProductId = product.Id,
            Quantity = 15
        };
        _context.StockReservationItems.Add(existingItem);
        await _context.SaveChangesAsync();

        // Tentar reservar mais 10 unidades (total 25, mas só tem 20 em estoque)
        var request = new CreateStockReservationRequest
        {
            InvoiceId = Guid.NewGuid(),
            Items = new List<CreateStockReservationItemRequest>
            {
                new() { ProductId = product.Id, Quantity = 10 }
            }
        };

        var contextMock = new Mock<ConsumeContext<CreateStockReservationRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        
        Result<StockReservationResponse>? capturedResponse = null;
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<StockReservationResponse>>()))
            .Callback<Result<StockReservationResponse>>(r => capturedResponse = r)
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert - Deve falhar porque disponível = 20 - 15 = 5, mas quer 10
        capturedResponse.Should().NotBeNull();
        capturedResponse!.IsSuccess.Should().BeFalse();
        capturedResponse.ErrorCode.Should().Be(ErrorCode.INSUFFICIENT_STOCK);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
