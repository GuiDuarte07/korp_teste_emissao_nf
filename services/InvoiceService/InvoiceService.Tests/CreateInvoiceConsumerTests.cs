using FluentAssertions;
using InvoiceService.Consumers;
using InvoiceService.Domain.Entities;
using InvoiceService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts.Invoice;
using Shared.Common;
using Xunit;

namespace InvoiceService.Tests.Consumers;

public class CreateInvoiceConsumerTests : IDisposable
{
    private readonly InvoiceDbContext _context;
    private readonly CreateInvoiceConsumer _consumer;
    private readonly Mock<ILogger<CreateInvoiceConsumer>> _loggerMock;

    public CreateInvoiceConsumerTests()
    {
        // Setup InMemory database com nome único para cada teste
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new InvoiceDbContext(options);
        _loggerMock = new Mock<ILogger<CreateInvoiceConsumer>>();
        _consumer = new CreateInvoiceConsumer(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldCreateInvoice_WhenValidRequest()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = productId, Quantity = 10 }
            }
        };

        var contextMock = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        var invoice = await _context.Invoices.Include(i => i.Items).FirstOrDefaultAsync();
        invoice.Should().NotBeNull();
        invoice!.InvoiceNumber.Should().Be(1);
        invoice.Items.Should().HaveCount(1);
        invoice.Items.First().ProductId.Should().Be(productId);
        invoice.Items.First().Quantity.Should().Be(10);
        invoice.Status.Should().Be(InvoiceStatus.Open);
        invoice.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Consume_ShouldGenerateSequentialNumbers()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        
        var request1 = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = productId1, Quantity = 1 }
            }
        };

        var request2 = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = productId2, Quantity = 2 }
            }
        };

        var contextMock1 = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock1.Setup(c => c.Message).Returns(request1);

        var contextMock2 = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock2.Setup(c => c.Message).Returns(request2);

        // Act
        await _consumer.Consume(contextMock1.Object);
        await _consumer.Consume(contextMock2.Object);

        // Assert
        var invoices = await _context.Invoices.OrderBy(i => i.InvoiceNumber).ToListAsync();
        invoices.Should().HaveCount(2);
        invoices[0].InvoiceNumber.Should().Be(1);
        invoices[1].InvoiceNumber.Should().Be(2);
    }

    [Fact]
    public async Task Consume_WithIdempotencyKey_ShouldCreateOnlyOnce()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = productId, Quantity = 5 }
            },
            IdempotencyKey = idempotencyKey
        };

        var contextMock1 = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock1.Setup(c => c.Message).Returns(request);
        contextMock1.Setup(c => c.RespondAsync(It.IsAny<Result<InvoiceDto>>()))
            .Returns(Task.CompletedTask);

        var contextMock2 = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock2.Setup(c => c.Message).Returns(request);
        contextMock2.Setup(c => c.RespondAsync(It.IsAny<Result<InvoiceDto>>()))
            .Returns(Task.CompletedTask);

        // Act - Enviar mesma requisição duas vezes
        await _consumer.Consume(contextMock1.Object);
        await _consumer.Consume(contextMock2.Object);

        // Assert - Deve criar apenas UMA nota fiscal
        var invoices = await _context.Invoices.ToListAsync();
        invoices.Should().HaveCount(1, "idempotency key deve prevenir duplicatas");
        
        var idempotencyKeys = await _context.IdempotencyKeys.ToListAsync();
        idempotencyKeys.Should().HaveCount(1, "deve haver apenas uma entrada de idempotency key");
        
        // Segunda chamada deve ter retornado a mesma invoice
        contextMock1.Verify(c => c.RespondAsync(It.IsAny<Result<InvoiceDto>>()), Times.Once);
        contextMock2.Verify(c => c.RespondAsync(It.IsAny<Result<InvoiceDto>>()), Times.Once);
    }

    [Fact]
    public async Task Consume_WithExistingIdempotencyKey_ShouldReturnExistingInvoice()
    {
        // Arrange - Criar invoice e chave de idempotência existentes
        var productId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = 1,
            Status = InvoiceStatus.Open,
            CreatedAt = DateTime.UtcNow,
            Items = new List<InvoiceItem>
            {
                new() { 
                    Id = Guid.NewGuid(),
                    ProductId = productId, 
                    ProductCode = "PROD001",
                    ProductDescription = "Produto Teste",
                    Quantity = 10 
                }
            }
        };
        
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        var existingKey = new IdempotencyKey
        {
            Id = Guid.NewGuid(),
            Key = "existing-key-123",
            InvoiceId = invoice.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };
        
        _context.IdempotencyKeys.Add(existingKey);
        await _context.SaveChangesAsync();

        // Act - Tentar criar nova invoice com chave existente
        var productId2 = Guid.NewGuid();
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = productId2, Quantity = 5 }
            },
            IdempotencyKey = "existing-key-123"
        };

        var contextMock = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<InvoiceDto>>()))
            .Returns(Task.CompletedTask);

        await _consumer.Consume(contextMock.Object);

        // Assert - Deve retornar a invoice existente, não criar nova
        var allInvoices = await _context.Invoices.ToListAsync();
        allInvoices.Should().HaveCount(1, "não deve criar nova invoice");
        allInvoices.First().Id.Should().Be(invoice.Id, "deve retornar a invoice existente");
        allInvoices.First().InvoiceNumber.Should().Be(1);
    }

    [Fact]
    public async Task Consume_WithoutIdempotencyKey_ShouldAllowMultipleCreations()
    {
        // Arrange - Requisições sem idempotency key
        var productId1 = Guid.NewGuid();
        var request1 = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = productId1, Quantity = 1 }
            }
            // Sem IdempotencyKey
        };

        var productId2 = Guid.NewGuid();
        var request2 = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = productId2, Quantity = 1 }
            }
            // Sem IdempotencyKey
        };

        var contextMock1 = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock1.Setup(c => c.Message).Returns(request1);
        contextMock1.Setup(c => c.RespondAsync(It.IsAny<Result<InvoiceDto>>()))
            .Returns(Task.CompletedTask);

        var contextMock2 = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock2.Setup(c => c.Message).Returns(request2);
        contextMock2.Setup(c => c.RespondAsync(It.IsAny<Result<InvoiceDto>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock1.Object);
        await _consumer.Consume(contextMock2.Object);

        // Assert - Deve criar DUAS notas fiscais diferentes
        var invoices = await _context.Invoices.ToListAsync();
        invoices.Should().HaveCount(2, "sem idempotency key, deve criar múltiplas invoices");
        invoices[0].Id.Should().NotBe(invoices[1].Id);
    }

    [Fact]
    public async Task Consume_ShouldSetCorrectExpirationTime()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var productId = Guid.NewGuid();
        
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = productId, Quantity = 1 }
            },
            IdempotencyKey = idempotencyKey
        };

        var contextMock = new Mock<ConsumeContext<CreateInvoiceRequest>>();
        contextMock.Setup(c => c.Message).Returns(request);
        contextMock.Setup(c => c.RespondAsync(It.IsAny<Result<InvoiceDto>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        var key = await _context.IdempotencyKeys.FirstOrDefaultAsync();
        key.Should().NotBeNull();
        key!.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromSeconds(2));
        key.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}