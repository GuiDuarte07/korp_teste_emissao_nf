namespace Shared.Contracts.Invoice;

/// <summary>
/// Atualiza o snapshot dos produtos na invoice após criação da reserva.
/// Usado em padrão fire-and-forget (Publish) pelo SAGA.
/// </summary>
public class UpdateInvoiceSnapshotRequest
{
    public Guid InvoiceId { get; set; }
    public Guid ReservationId { get; set; }
    public List<InvoiceItemSnapshotData> Items { get; set; } = new();
}

public class InvoiceItemSnapshotData
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
}
