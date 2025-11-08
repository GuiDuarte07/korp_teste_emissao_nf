using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services.Interfaces;
using Shared.Common;
using Shared.Contracts.Inventory;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var result = await _inventoryService.GetAllProductsAsync();
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produtos");
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao buscar produtos" });
            }
        }

        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            try
            {
                var result = await _inventoryService.GetProductByIdAsync(id);
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar produto {ProductId}", id);
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao buscar produto" });
            }
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                var result = await _inventoryService.CreateProductAsync(request);
                
                if (result.IsSuccess)
                {
                    return result.ToCreatedResult(nameof(GetProductById), new { id = result.Data!.Id });
                }
                
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar produto {ProductCode}", request.Code);
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao criar produto" });
            }
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var result = await _inventoryService.UpdateProductAsync(id, request);
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar produto {ProductId}", id);
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao atualizar produto" });
            }
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            try
            {
                var result = await _inventoryService.DeleteProductAsync(id);
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar produto {ProductId}", id);
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao deletar produto" });
            }
        }

        [HttpPost("reservations")]
        public async Task<IActionResult> CreateReservation([FromBody] CreateStockReservationRequest request)
        {
            try
            {
                var result = await _inventoryService.CreateReservationAsync(request);
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar reserva para nota fiscal {InvoiceId}", request.InvoiceId);
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao criar reserva" });
            }
        }

        [HttpPost("reservations/{id}/confirm")]
        public async Task<IActionResult> ConfirmReservation(Guid id)
        {
            try
            {
                var result = await _inventoryService.ConfirmReservationAsync(id);
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao confirmar reserva {ReservationId}", id);
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao confirmar reserva" });
            }
        }

        [HttpPost("reservations/{id}/cancel")]
        public async Task<IActionResult> CancelReservation(Guid id)
        {
            try
            {
                var result = await _inventoryService.CancelReservationAsync(id);
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar reserva {ReservationId}", id);
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao cancelar reserva" });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var result = await _inventoryService.GetHealthAsync();
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar health");
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao verificar saúde do serviço" });
            }
        }

        [HttpGet("stock-status")]
        public async Task<IActionResult> GetStockStatus()
        {
            try
            {
                var result = await _inventoryService.GetStockStatusAsync();
                return result.ToActionResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar status do estoque");
                return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao buscar status do estoque" });
            }
        }
    }
}
