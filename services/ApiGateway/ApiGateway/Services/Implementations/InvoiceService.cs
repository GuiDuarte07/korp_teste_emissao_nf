using MassTransit;
using Shared.Common;
using Shared.Contracts.Inventory;
using Shared.Contracts.Invoice;
using ApiGateway.Services.Interfaces;

namespace ApiGateway.Services.Implementations;

public class InvoiceService : IInvoiceService
{
    private readonly IRequestClient<GetAllInvoicesRequest> _getAllInvoicesClient;
    private readonly IRequestClient<GetInvoiceByIdRequest> _getInvoiceByIdClient;
    private readonly IRequestClient<CreateInvoiceRequest> _createInvoiceClient;
    private readonly IRequestClient<DeleteInvoiceRequest> _deleteInvoiceClient;
    private readonly IRequestClient<PrintInvoiceRequest> _printInvoiceClient;
    private readonly IRequestClient<GenerateInvoicePdfRequest> _generatePdfClient;
    private readonly IRequestClient<GetProductByIdRequest> _getProductClient;
    private readonly IRequestClient<CreateStockReservationRequest> _createReservationClient;
    private readonly IRequestClient<CancelReservationRequest> _cancelReservationClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        IRequestClient<GetAllInvoicesRequest> getAllInvoicesClient,
        IRequestClient<GetInvoiceByIdRequest> getInvoiceByIdClient,
        IRequestClient<CreateInvoiceRequest> createInvoiceClient,
        IRequestClient<DeleteInvoiceRequest> deleteInvoiceClient,
        IRequestClient<PrintInvoiceRequest> printInvoiceClient,
        IRequestClient<GenerateInvoicePdfRequest> generatePdfClient,
        IRequestClient<GetProductByIdRequest> getProductClient,
        IRequestClient<CreateStockReservationRequest> createReservationClient,
        IRequestClient<CancelReservationRequest> cancelReservationClient,
        IPublishEndpoint publishEndpoint,
        ILogger<InvoiceService> logger)
    {
        _getAllInvoicesClient = getAllInvoicesClient;
        _getInvoiceByIdClient = getInvoiceByIdClient;
        _createInvoiceClient = createInvoiceClient;
        _deleteInvoiceClient = deleteInvoiceClient;
        _printInvoiceClient = printInvoiceClient;
        _generatePdfClient = generatePdfClient;
        _getProductClient = getProductClient;
        _createReservationClient = createReservationClient;
        _cancelReservationClient = cancelReservationClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Result<List<InvoiceDto>>> GetAllInvoicesAsync(GetAllInvoicesRequest request)
    {
        try
        {
            _logger.LogInformation("Buscando notas fiscais com filtros: Status={Status}, IncludeCancelled={IncludeCancelled}, CreatedFrom={CreatedFrom}, CreatedTo={CreatedTo}",
                request.Status, request.IncludeCancelled, request.CreatedFrom, request.CreatedTo);

            var response = await _getAllInvoicesClient.GetResponse<Result<List<InvoiceDto>>>(
                request,
                timeout: RequestTimeout.After(m: 1));

            return response.Message;
        }
        catch (RequestTimeoutException ex)
        {
            _logger.LogError(ex, "Timeout ao buscar notas fiscais");
            return Result<List<InvoiceDto>>.Failure(ErrorCode.INTERNAL_ERROR, "Timeout ao buscar notas fiscais");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar notas fiscais");
            return Result<List<InvoiceDto>>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao buscar notas fiscais");
        }
    }

    public async Task<Result<InvoiceDto>> GetInvoiceByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Buscando nota fiscal {InvoiceId}", id);

            var response = await _getInvoiceByIdClient.GetResponse<Result<InvoiceDto>>(
                new GetInvoiceByIdRequest { Id = id },
                timeout: RequestTimeout.After(m: 1));

            return response.Message;
        }
        catch (RequestTimeoutException ex)
        {
            _logger.LogError(ex, "Timeout ao buscar nota fiscal {InvoiceId}", id);
            return Result<InvoiceDto>.Failure(ErrorCode.INTERNAL_ERROR, "Timeout ao buscar nota fiscal");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar nota fiscal {InvoiceId}", id);
            return Result<InvoiceDto>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao buscar nota fiscal");
        }
    }

    public async Task<Result<InvoiceDto>> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        _logger.LogInformation("=== INICIANDO SAGA DE CRIAÇÃO DE NOTA FISCAL ===");
        _logger.LogInformation("Criando nota fiscal com {ItemCount} itens", request.Items.Count);

        // Validação: Verificar se há produtos duplicados
        var duplicateProducts = request.Items
            .GroupBy(i => i.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateProducts.Any())
        {
            _logger.LogWarning("Tentativa de criar nota fiscal com produtos duplicados: {ProductIds}", 
                string.Join(", ", duplicateProducts));
            return Result<InvoiceDto>.Failure(
                ErrorCode.VALIDATION_ERROR, 
                "Não é permitido adicionar o mesmo produto mais de uma vez na nota fiscal");
        }

        // SAGA Step 1: Criar nota fiscal (Open)
        Result<InvoiceDto>? invoiceResult = null;
        try
        {
            _logger.LogInformation("[SAGA] Passo 1: Criando nota fiscal no InvoiceService");

            var invoiceResponse = await _createInvoiceClient.GetResponse<Result<InvoiceDto>>(
                request,
                timeout: RequestTimeout.After(m: 1));

            invoiceResult = invoiceResponse.Message;

            if (!invoiceResult.IsSuccess)
            {
                _logger.LogError("[SAGA] Falha ao criar nota fiscal: {Error}", invoiceResult.ErrorMessage);
                return invoiceResult;
            }

            _logger.LogInformation("[SAGA] Nota fiscal #{InvoiceNumber} criada com sucesso", invoiceResult.Data!.InvoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SAGA] Erro ao criar nota fiscal");
            return Result<InvoiceDto>.Failure(ErrorCode.INTERNAL_ERROR, $"Erro ao criar nota fiscal: {ex.Message}");
        }

        // SAGA Step 2: Criar reserva de estoque
        Guid? reservationId = null;
        try
        {
            _logger.LogInformation("[SAGA] Passo 2: Criando reserva de estoque no InventoryService");

            var reservationRequest = new CreateStockReservationRequest
            {
                InvoiceId = invoiceResult.Data!.Id,
                Items = request.Items.Select(i => new CreateStockReservationItemRequest
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            var reservationResponse = await _createReservationClient.GetResponse<Result<StockReservationResponse>>(
                reservationRequest,
                timeout: RequestTimeout.After(m: 1));

            if (!reservationResponse.Message.IsSuccess)
            {
                _logger.LogError("[SAGA] Falha ao criar reserva: {Error}", reservationResponse.Message.ErrorMessage);
                
                // COMPENSAÇÃO: Deletar a nota fiscal criada
                await CompensateInvoiceCreation(invoiceResult.Data!.Id);

                return Result<InvoiceDto>.Failure(
                    reservationResponse.Message.ErrorCode ?? ErrorCode.INTERNAL_ERROR,
                    $"Falha ao reservar estoque: {reservationResponse.Message.ErrorMessage}");
            }

            reservationId = reservationResponse.Message.Data!.Id;
            _logger.LogInformation("[SAGA] Reserva {ReservationId} criada com sucesso", reservationId);

            // SAGA Step 3: Atualizar snapshot da invoice (fire-and-forget)
            _logger.LogInformation("[SAGA] Passo 3: Publicando atualização de snapshot da invoice");
            
            var snapshotRequest = new UpdateInvoiceSnapshotRequest
            {
                InvoiceId = invoiceResult.Data!.Id,
                ReservationId = reservationId.Value,
                Items = reservationResponse.Message.Data.Items.Select(i => new InvoiceItemSnapshotData
                {
                    ProductId = i.ProductId,
                    ProductCode = i.ProductCode,
                    ProductDescription = i.ProductDescription
                }).ToList()
            };

            await _publishEndpoint.Publish(snapshotRequest);
            _logger.LogInformation("[SAGA] Evento de atualização de snapshot publicado (fire-and-forget)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SAGA] Erro ao criar reserva de estoque");
            
            // COMPENSAÇÃO: Deletar a nota fiscal criada
            await CompensateInvoiceCreation(invoiceResult.Data!.Id);

            return Result<InvoiceDto>.Failure(ErrorCode.INTERNAL_ERROR, $"Erro ao reservar estoque: {ex.Message}");
        }

        _logger.LogInformation("=== SAGA COMPLETADA COM SUCESSO ===");
        _logger.LogInformation("Nota fiscal #{InvoiceNumber} criada e estoque reservado", invoiceResult.Data!.InvoiceNumber);

        return invoiceResult;
    }

    private async Task CompensateInvoiceCreation(Guid invoiceId)
    {
        try
        {
            _logger.LogWarning("[SAGA COMPENSAÇÃO] Deletando nota fiscal {InvoiceId}", invoiceId);

            var deleteResponse = await _deleteInvoiceClient.GetResponse<Result>(
                new DeleteInvoiceRequest { Id = invoiceId },
                timeout: RequestTimeout.After(m: 1));

            if (deleteResponse.Message.IsSuccess)
            {
                _logger.LogInformation("[SAGA COMPENSAÇÃO] Nota fiscal {InvoiceId} deletada com sucesso", invoiceId);
            }
            else
            {
                _logger.LogError("[SAGA COMPENSAÇÃO] Falha ao deletar nota fiscal {InvoiceId}: {Error}",
                    invoiceId, deleteResponse.Message.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SAGA COMPENSAÇÃO] Erro ao deletar nota fiscal {InvoiceId}", invoiceId);
        }
    }

    public async Task<Result> DeleteInvoiceAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Deletando nota fiscal {InvoiceId}", id);

            var response = await _deleteInvoiceClient.GetResponse<Result>(
                new DeleteInvoiceRequest { Id = id },
                timeout: RequestTimeout.After(m: 1));

            return response.Message;
        }
        catch (RequestTimeoutException ex)
        {
            _logger.LogError(ex, "Timeout ao deletar nota fiscal {InvoiceId}", id);
            return Result.Failure(ErrorCode.INTERNAL_ERROR, "Timeout ao deletar nota fiscal");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar nota fiscal {InvoiceId}", id);
            return Result.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao deletar nota fiscal");
        }
    }

    public async Task<Result<InvoiceDto>> PrintInvoiceAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Imprimindo nota fiscal {InvoiceId}", id);

            var response = await _printInvoiceClient.GetResponse<Result<InvoiceDto>>(
                new PrintInvoiceRequest { Id = id },
                timeout: RequestTimeout.After(m: 1));

            return response.Message;
        }
        catch (RequestTimeoutException ex)
        {
            _logger.LogError(ex, "Timeout ao imprimir nota fiscal {InvoiceId}", id);
            return Result<InvoiceDto>.Failure(ErrorCode.INTERNAL_ERROR, "Timeout ao imprimir nota fiscal");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir nota fiscal {InvoiceId}", id);
            return Result<InvoiceDto>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao imprimir nota fiscal");
        }
    }

    public async Task<Result<byte[]>> GenerateInvoicePdfAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Gerando PDF da nota fiscal {InvoiceId}", id);

            var response = await _generatePdfClient.GetResponse<Result<byte[]>>(
                new GenerateInvoicePdfRequest { InvoiceId = id },
                timeout: RequestTimeout.After(m: 1));

            return response.Message;
        }
        catch (RequestTimeoutException ex)
        {
            _logger.LogError(ex, "Timeout ao gerar PDF da nota fiscal {InvoiceId}", id);
            return Result<byte[]>.Failure(ErrorCode.INTERNAL_ERROR, "Timeout ao gerar PDF");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF da nota fiscal {InvoiceId}", id);
            return Result<byte[]>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao gerar PDF");
        }
    }
}
