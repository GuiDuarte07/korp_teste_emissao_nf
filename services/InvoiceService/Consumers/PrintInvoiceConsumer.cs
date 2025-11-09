using InvoiceService.Domain.Entities;
using InvoiceService.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Contracts.Inventory;
using Shared.Contracts.Invoice;

namespace InvoiceService.Consumers;

public class PrintInvoiceConsumer : IConsumer<PrintInvoiceRequest>
{
    private readonly InvoiceDbContext _context;
    private readonly IRequestClient<ConfirmReservationRequest> _confirmReservationClient;
    private readonly ILogger<PrintInvoiceConsumer> _logger;

    public PrintInvoiceConsumer(
        InvoiceDbContext context,
        IRequestClient<ConfirmReservationRequest> confirmReservationClient,
        ILogger<PrintInvoiceConsumer> logger)
    {
        _context = context;
        _confirmReservationClient = confirmReservationClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PrintInvoiceRequest> context)
    {
        try
        {

            Task.Delay(5000).Wait(); // Simula tempo de impressão

            _logger.LogInformation("Imprimindo nota fiscal {InvoiceId}", context.Message.Id);

            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == context.Message.Id);

            if (invoice == null)
            {
                _logger.LogWarning("Nota fiscal {InvoiceId} não encontrada", context.Message.Id);
                await context.RespondAsync(Result<InvoiceDto>.Failure(
                    ErrorCode.NOT_FOUND,
                    $"Nota fiscal {context.Message.Id} não encontrada"));
                return;
            }

            // Valida se a nota está Open
            if (invoice.Status == InvoiceStatus.Closed)
            {
                _logger.LogWarning("Tentativa de imprimir nota fiscal já fechada {InvoiceNumber}", invoice.InvoiceNumber);
                await context.RespondAsync(Result<InvoiceDto>.Failure(
                    ErrorCode.INVALID_REQUEST,
                    "Nota fiscal já foi impressa anteriormente"));
                return;
            }

            // Confirma a reserva (debita estoque no InventoryService)
            var reservationId = invoice.Items.FirstOrDefault()?.ReservationId;
            
            if (!reservationId.HasValue)
            {
                _logger.LogWarning("Nota fiscal #{InvoiceNumber} não possui reserva associada", invoice.InvoiceNumber);
                await context.RespondAsync(Result<InvoiceDto>.Failure(
                    ErrorCode.INVALID_REQUEST,
                    "Nota fiscal não possui reserva de estoque associada"));
                return;
            }

            _logger.LogInformation("Confirmando reserva {ReservationId} da nota #{InvoiceNumber}",
                reservationId.Value, invoice.InvoiceNumber);

            try
            {
                var confirmResponse = await _confirmReservationClient.GetResponse<Result<StockReservationResponse>>(
                    new ConfirmReservationRequest { ReservationId = reservationId.Value },
                    timeout: RequestTimeout.After(s: 30));

                if (!confirmResponse.Message.IsSuccess)
                {
                    _logger.LogError("Falha ao confirmar reserva {ReservationId}: {Error}",
                        reservationId.Value, confirmResponse.Message.ErrorMessage);

                    await context.RespondAsync(Result<InvoiceDto>.Failure(
                        confirmResponse.Message.ErrorCode ?? ErrorCode.INTERNAL_ERROR,
                        $"Falha ao confirmar reserva: {confirmResponse.Message.ErrorMessage}"));
                    return;
                }

                _logger.LogInformation("Reserva {ReservationId} confirmada com sucesso", reservationId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar reserva {ReservationId}", reservationId.Value);
                await context.RespondAsync(Result<InvoiceDto>.Failure(
                    ErrorCode.INTERNAL_ERROR,
                    $"Erro ao confirmar reserva: {ex.Message}"));
                return;
            }

            // Atualiza status da nota para Closed
            invoice.Status = InvoiceStatus.Closed;
            invoice.PrintedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Nota fiscal #{InvoiceNumber} impressa e fechada com sucesso", invoice.InvoiceNumber);

            var invoiceDto = new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Status = "Closed",
                CreatedAt = invoice.CreatedAt,
                PrintedAt = invoice.PrintedAt,
                Cancelled = invoice.Cancelled,
                CancelledAt = invoice.CancelledAt,
                Items = invoice.Items.Select(i => new InvoiceItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductCode = i.ProductCode,
                    ProductDescription = i.ProductDescription,
                    Quantity = i.Quantity
                }).ToList()
            };

            await context.RespondAsync(Result<InvoiceDto>.Success(invoiceDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir nota fiscal {InvoiceId}", context.Message.Id);
            await context.RespondAsync(Result<InvoiceDto>.Failure(
                ErrorCode.INTERNAL_ERROR,
                "Erro ao imprimir nota fiscal"));
        }
    }
}
