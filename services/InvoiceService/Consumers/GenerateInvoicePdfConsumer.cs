using InvoiceService.Infrastructure.Data;
using InvoiceService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Contracts.Invoice;

namespace InvoiceService.Consumers;

public class GenerateInvoicePdfConsumer : IConsumer<GenerateInvoicePdfRequest>
{
    private readonly InvoiceDbContext _context;
    private readonly InvoicePdfGenerator _pdfGenerator;
    private readonly ILogger<GenerateInvoicePdfConsumer> _logger;

    public GenerateInvoicePdfConsumer(
        InvoiceDbContext context,
        InvoicePdfGenerator pdfGenerator,
        ILogger<GenerateInvoicePdfConsumer> logger)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GenerateInvoicePdfRequest> context)
    {
        try
        {
            _logger.LogInformation("Gerando PDF da nota fiscal {InvoiceId}", context.Message.InvoiceId);

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == context.Message.InvoiceId);

            if (invoice == null)
            {
                _logger.LogWarning("Nota fiscal {InvoiceId} não encontrada", context.Message.InvoiceId);
                await context.RespondAsync(Result<byte[]>.Failure(
                    ErrorCode.NOT_FOUND,
                    "Nota fiscal não encontrada"));
                return;
            }

            if (invoice.Cancelled)
            {
                _logger.LogWarning("Tentativa de gerar PDF de nota fiscal cancelada {InvoiceId}", context.Message.InvoiceId);
                await context.RespondAsync(Result<byte[]>.Failure(
                    ErrorCode.VALIDATION_ERROR,
                    "Não é possível gerar PDF de uma nota fiscal cancelada"));
                return;
            }

            // Gera o PDF
            var pdfBytes = _pdfGenerator.GenerateInvoicePdf(invoice);

            _logger.LogInformation("PDF gerado com sucesso para nota fiscal {InvoiceNumber}", invoice.InvoiceNumber);

            await context.RespondAsync(Result<byte[]>.Success(pdfBytes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF da nota fiscal {InvoiceId}", context.Message.InvoiceId);
            await context.RespondAsync(Result<byte[]>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao gerar PDF da nota fiscal"));
        }
    }
}
