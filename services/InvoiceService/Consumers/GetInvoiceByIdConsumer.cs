using InvoiceService.Domain.Entities;
using InvoiceService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Contracts.Invoice;

namespace InvoiceService.Consumers;

public class GetInvoiceByIdConsumer : IConsumer<GetInvoiceByIdRequest>
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<GetInvoiceByIdConsumer> _logger;

    public GetInvoiceByIdConsumer(InvoiceDbContext context, ILogger<GetInvoiceByIdConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetInvoiceByIdRequest> context)
    {
        try
        {
            _logger.LogInformation("Buscando nota fiscal {InvoiceId}", context.Message.Id);

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .Where(i => i.Id == context.Message.Id)
                .Select(i => new InvoiceDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    Status = i.Status == InvoiceStatus.Open ? "Open" : "Closed",
                    CreatedAt = i.CreatedAt,
                    PrintedAt = i.PrintedAt,
                    Items = i.Items.Select(item => new InvoiceItemDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductDescription = item.ProductDescription,
                        Quantity = item.Quantity
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (invoice == null)
            {
                _logger.LogWarning("Nota fiscal {InvoiceId} não encontrada", context.Message.Id);
                await context.RespondAsync(Result<InvoiceDto>.Failure(
                    ErrorCode.NOT_FOUND,
                    $"Nota fiscal {context.Message.Id} não encontrada"));
                return;
            }

            _logger.LogInformation("Nota fiscal {InvoiceNumber} encontrada", invoice.InvoiceNumber);
            await context.RespondAsync(Result<InvoiceDto>.Success(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar nota fiscal {InvoiceId}", context.Message.Id);
            await context.RespondAsync(Result<InvoiceDto>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao buscar nota fiscal"));
        }
    }
}
