namespace Shared.Contracts.Invoice;

public class GetAllInvoicesRequest
{
    /// Filtra por status da nota fiscal. Valores possíveis: "Open", "Closed". Se null, retorna todos os status.
    public string? Status { get; set; }
    
    /// <summary>
    /// Se true, retorna apenas notas canceladas.
    /// Se false, retorna apenas notas não canceladas.
    /// Se null, retorna todas (canceladas e não canceladas).
    /// </summary>
    public bool? IncludeCancelled { get; set; }
    
    /// Data inicial de criação (inclusive).
    public DateTime? CreatedFrom { get; set; }
    
    /// Data final de criação (inclusive).
    public DateTime? CreatedTo { get; set; }
}

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
