namespace Shared.Contracts.Invoice;

/// <summary>
/// Evento de domínio publicado quando uma invoice é cancelada.
/// InventoryService assina este evento para cancelar a reserva de estoque
/// </summary>
public class InvoiceCancelledEvent
{
    public Guid InvoiceId { get; set; }
    public int InvoiceNumber { get; set; }
    public DateTime CancelledAt { get; set; }
    public List<InvoiceCancelledItem> Items { get; set; } = new();
}

public class InvoiceCancelledItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
