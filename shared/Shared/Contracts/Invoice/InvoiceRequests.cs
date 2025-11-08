namespace Shared.Contracts.Invoice;

public class GetAllInvoicesRequest { }

public class GetInvoiceByIdRequest
{
    public Guid Id { get; set; }
}

public class DeleteInvoiceRequest
{
    public Guid Id { get; set; }
}

public class PrintInvoiceRequest
{
    public Guid Id { get; set; }
}
