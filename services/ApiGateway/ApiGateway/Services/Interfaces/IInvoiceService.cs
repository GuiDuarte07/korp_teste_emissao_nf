using Shared.Common;
using Shared.Contracts.Invoice;

namespace ApiGateway.Services.Interfaces;

public interface IInvoiceService
{
    Task<Result<List<InvoiceDto>>> GetAllInvoicesAsync();
    Task<Result<InvoiceDto>> GetInvoiceByIdAsync(Guid id);
    Task<Result<InvoiceDto>> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<Result> DeleteInvoiceAsync(Guid id);
    Task<Result<InvoiceDto>> PrintInvoiceAsync(Guid id);
}
