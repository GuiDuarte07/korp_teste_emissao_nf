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
            var request = context.Message;
            _logger.LogInformation("Buscando notas fiscais com filtros: Status={Status}, IncludeCancelled={IncludeCancelled}, CreatedFrom={CreatedFrom}, CreatedTo={CreatedTo}",
                request.Status, request.IncludeCancelled, request.CreatedFrom, request.CreatedTo);

            var query = _context.Invoices
                .Include(i => i.Items)
                .AsQueryable();

            // Filtro por status (Open/Closed)
            if (!string.IsNullOrEmpty(request.Status))
            {
                if (Enum.TryParse<InvoiceStatus>(request.Status, true, out var status))
                {
                    query = query.Where(i => i.Status == status);
                }
            }

            // Filtro por canceladas
            if (request.IncludeCancelled.HasValue)
            {
                query = query.Where(i => i.Cancelled == request.IncludeCancelled.Value);
            }

            // Filtro por data de criação
            if (request.CreatedFrom.HasValue)
            {
                query = query.Where(i => i.CreatedAt >= request.CreatedFrom.Value);
            }

            if (request.CreatedTo.HasValue)
            {
                var endOfDay = request.CreatedTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(i => i.CreatedAt <= endOfDay);
            }

            var invoices = await query
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => new InvoiceDto
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    Status = i.Status == InvoiceStatus.Open ? "Open" : "Closed",
                    CreatedAt = i.CreatedAt,
                    PrintedAt = i.PrintedAt,
                    Cancelled = i.Cancelled,
                    CancelledAt = i.CancelledAt,
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
