using InvoiceService.Domain.Entities;
using InvoiceService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Contracts.Invoice;

namespace InvoiceService.Consumers;

public class GetAllInvoicesConsumer : IConsumer<GetAllInvoicesRequest>
{
    private readonly InvoiceDbContext _context;
    private readonly ILogger<GetAllInvoicesConsumer> _logger;

    public GetAllInvoicesConsumer(InvoiceDbContext context, ILogger<GetAllInvoicesConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GetAllInvoicesRequest> context)
    {
        try
        {
            _logger.LogInformation("Buscando todas as notas fiscais");

            var invoices = await _context.Invoices
                .Include(i => i.Items)
                .OrderByDescending(i => i.InvoiceNumber)
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
                .ToListAsync();

            _logger.LogInformation("Encontradas {Count} notas fiscais", invoices.Count);

            await context.RespondAsync(Result<List<InvoiceDto>>.Success(invoices));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar notas fiscais");
            await context.RespondAsync(Result<List<InvoiceDto>>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao buscar notas fiscais"));
        }
    }
}
