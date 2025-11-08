using Shared.Common;
using Shared.Contracts.Inventory;

namespace ApiGateway.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<Result<List<ProductDto>>> GetAllProductsAsync();
        Task<Result<ProductDto>> GetProductByIdAsync(Guid id);
        Task<Result<CreateProductResponse>> CreateProductAsync(CreateProductRequest request);
        Task<Result<ProductDto>> UpdateProductAsync(Guid id, UpdateProductRequest request);
        Task<Result> DeleteProductAsync(Guid id);
        Task<Result<StockReservationResponse>> CreateReservationAsync(CreateStockReservationRequest request);
        Task<Result<StockReservationResponse>> ConfirmReservationAsync(Guid reservationId);
        Task<Result<StockReservationResponse>> CancelReservationAsync(Guid reservationId);
        Task<Result<HealthCheckResponse>> GetHealthAsync();
        Task<Result<StockStatusResponse>> GetStockStatusAsync();
    }
}
