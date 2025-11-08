namespace InventoryService.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool Confirmed { get; set; } = false;
    public bool Cancelled { get; set; } = false;

    public ICollection<StockReservationItem> Items { get; set; } = new List<StockReservationItem>();
}
