using ApiGateway.Services.Interfaces;
using MassTransit;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace ApiGateway.Services.Implementations
{
    public class InventoryService : IInventoryService
    {
        private readonly IRequestClient<GetAllProductsRequest> _getAllProductsClient;
        private readonly IRequestClient<GetProductByIdRequest> _getProductByIdClient;
        private readonly IRequestClient<CreateProductRequest> _createProductClient;
        private readonly IRequestClient<UpdateProductRequest> _updateProductClient;
        private readonly IRequestClient<DeleteProductRequest> _deleteProductClient;
        private readonly IRequestClient<CreateStockReservationRequest> _createReservationClient;
        private readonly IRequestClient<ConfirmReservationRequest> _confirmReservationClient;
        private readonly IRequestClient<CancelReservationRequest> _cancelReservationClient;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(
            IRequestClient<GetAllProductsRequest> getAllProductsClient,
            IRequestClient<GetProductByIdRequest> getProductByIdClient,
            IRequestClient<CreateProductRequest> createProductClient,
            IRequestClient<UpdateProductRequest> updateProductClient,
            IRequestClient<DeleteProductRequest> deleteProductClient,
            IRequestClient<CreateStockReservationRequest> createReservationClient,
            IRequestClient<ConfirmReservationRequest> confirmReservationClient,
            IRequestClient<CancelReservationRequest> cancelReservationClient,
            ILogger<InventoryService> logger)
        {
            _getAllProductsClient = getAllProductsClient;
            _getProductByIdClient = getProductByIdClient;
            _createProductClient = createProductClient;
            _updateProductClient = updateProductClient;
            _deleteProductClient = deleteProductClient;
            _createReservationClient = createReservationClient;
            _confirmReservationClient = confirmReservationClient;
            _cancelReservationClient = cancelReservationClient;
            _logger = logger;
        }

        public async Task<Result<List<ProductDto>>> GetAllProductsAsync()
        {
            try
            {
                var response = await _getAllProductsClient.GetResponse<Result<List<ProductDto>>>(new GetAllProductsRequest());
                return response.Message;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao buscar todos os produtos");
                return Result<List<ProductDto>>.Failure(ErrorCode.INTERNAL_ERROR, "Serviço de inventário não disponível");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os produtos");
                return Result<List<ProductDto>>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao comunicar com serviço de inventário");
            }
        }

        public async Task<Result<ProductDto>> GetProductByIdAsync(Guid id)
        {
            try
            {
                var response = await _getProductByIdClient.GetResponse<Result<ProductDto>>(new GetProductByIdRequest { Id = id });
                return response.Message;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao buscar produto {ProductId}", id);
                return Result<ProductDto>.Failure(ErrorCode.INTERNAL_ERROR, "Serviço de inventário não disponível");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produto {ProductId}", id);
                return Result<ProductDto>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao comunicar com serviço de inventário");
            }
        }

        public async Task<Result<CreateProductResponse>> CreateProductAsync(CreateProductRequest request)
        {
            try
            {
                var response = await _createProductClient.GetResponse<Result<CreateProductResponse>>(request);
                return response.Message;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao criar produto {ProductCode}", request.Code);
                return Result<CreateProductResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Serviço de inventário não disponível");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar produto {ProductCode}", request.Code);
                return Result<CreateProductResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao comunicar com serviço de inventário");
            }
        }

        public async Task<Result<ProductDto>> UpdateProductAsync(Guid id, UpdateProductRequest request)
        {
            try
            {
                request.Id = id;
                var response = await _updateProductClient.GetResponse<Result<ProductDto>>(request);
                return response.Message;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao atualizar produto {ProductId}", id);
                return Result<ProductDto>.Failure(ErrorCode.INTERNAL_ERROR, "Serviço de inventário não disponível");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar produto {ProductId}", id);
                return Result<ProductDto>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao comunicar com serviço de inventário");
            }
        }

        public async Task<Result> DeleteProductAsync(Guid id)
        {
            try
            {
                var response = await _deleteProductClient.GetResponse<Result>(new DeleteProductRequest { Id = id });
                return response.Message;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao deletar produto {ProductId}", id);
                return Result.Failure(ErrorCode.INTERNAL_ERROR, "Serviço de inventário não disponível");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar produto {ProductId}", id);
                return Result.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao comunicar com serviço de inventário");
            }
        }

        public async Task<Result<StockReservationResponse>> CreateReservationAsync(CreateStockReservationRequest request)
        {
            try
            {
                var response = await _createReservationClient.GetResponse<Result<StockReservationResponse>>(request);
                return response.Message;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao criar reserva para invoice {InvoiceId}", request.InvoiceId);
                return Result<StockReservationResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Serviço de inventário não disponível");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar reserva para invoice {InvoiceId}", request.InvoiceId);
                return Result<StockReservationResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao comunicar com serviço de inventário");
            }
        }

        public async Task<Result<StockReservationResponse>> ConfirmReservationAsync(Guid reservationId)
        {
            try
            {
                var response = await _confirmReservationClient.GetResponse<Result<StockReservationResponse>>(
                    new ConfirmReservationRequest { ReservationId = reservationId });
                return response.Message;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao confirmar reserva {ReservationId}", reservationId);
                return Result<StockReservationResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Serviço de inventário não disponível");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar reserva {ReservationId}", reservationId);
                return Result<StockReservationResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao comunicar com serviço de inventário");
            }
        }

        public async Task<Result<StockReservationResponse>> CancelReservationAsync(Guid reservationId)
        {
            try
            {
                var response = await _cancelReservationClient.GetResponse<Result<StockReservationResponse>>(
                    new CancelReservationRequest { InvoiceId = reservationId });
                return response.Message;
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao cancelar reserva {ReservationId}", reservationId);
                return Result<StockReservationResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Serviço de inventário não disponível");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar reserva {ReservationId}", reservationId);
                return Result<StockReservationResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao comunicar com serviço de inventário");
            }
        }

        public async Task<Result<HealthCheckResponse>> GetHealthAsync()
        {
            try
            {
                var response = await _getAllProductsClient.GetResponse<Result<List<ProductDto>>>(
                    new GetAllProductsRequest(), 
                    timeout: RequestTimeout.After(s: 5));
                
                return Result<HealthCheckResponse>.Success(new HealthCheckResponse
                {
                    Status = "Healthy",
                    DatabaseConnected = true,
                    QueueConnected = true,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch
            {
                return Result<HealthCheckResponse>.Success(new HealthCheckResponse
                {
                    Status = "Unhealthy",
                    DatabaseConnected = false,
                    QueueConnected = false,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        public async Task<Result<StockStatusResponse>> GetStockStatusAsync()
        {
            try
            {
                var productsResult = await GetAllProductsAsync();
                
                if (!productsResult.IsSuccess || productsResult.ErrorCode.HasValue)
                {
                    return Result<StockStatusResponse>.Failure(
                        productsResult.ErrorCode ?? ErrorCode.INTERNAL_ERROR, 
                        productsResult.ErrorMessage!);
                }

                var totalStock = productsResult.Data!.Sum(p => p.Stock);
                var totalReserved = productsResult.Data!.Sum(p => p.ReservedStock);
                var totalAvailable = productsResult.Data!.Sum(p => p.AvailableStock);
                
                var response = new StockStatusResponse
                {
                    TotalProducts = productsResult.Data!.Count,
                    TotalStock = totalStock,
                    TotalReserved = totalReserved,
                    TotalAvailable = totalAvailable
                };

                return Result<StockStatusResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter status do estoque");
                return Result<StockStatusResponse>.Failure(ErrorCode.INTERNAL_ERROR, "Erro ao obter status do estoque");
            }
        }
    }
}
