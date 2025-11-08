using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services.Interfaces;
using Shared.Common;
using Shared.Contracts.Invoice;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(IInvoiceService invoiceService, ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllInvoices([FromQuery] GetAllInvoicesRequest request)
    {
        try
        {
            var result = await _invoiceService.GetAllInvoicesAsync(request);
            return result.ToActionResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar notas fiscais");
            return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao buscar notas fiscais" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvoiceById(Guid id)
    {
        try
        {
            var result = await _invoiceService.GetInvoiceByIdAsync(id);
            return result.ToActionResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar nota fiscal {InvoiceId}", id);
            return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao buscar nota fiscal" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        try
        {
            var result = await _invoiceService.CreateInvoiceAsync(request);
            
            if (result.IsSuccess)
            {
                return result.ToCreatedResult(nameof(GetInvoiceById), new { id = result.Data!.Id });
            }
            
            return result.ToActionResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar nota fiscal");
            return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao criar nota fiscal" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInvoice(Guid id)
    {
        try
        {
            var result = await _invoiceService.DeleteInvoiceAsync(id);
            return result.ToActionResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar nota fiscal {InvoiceId}", id);
            return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao deletar nota fiscal" });
        }
    }

    [HttpPost("{id}/print")]
    public async Task<IActionResult> PrintInvoice(Guid id)
    {
        try
        {
            var result = await _invoiceService.PrintInvoiceAsync(id);
            return result.ToActionResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao imprimir nota fiscal {InvoiceId}", id);
            return StatusCode(500, new { ErrorCode = ErrorCode.INTERNAL_ERROR.ToString(), ErrorMessage = "Erro interno ao imprimir nota fiscal" });
        }
    }
}
