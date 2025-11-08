namespace InventoryService.Domain.Entities;

public class StockReservationItem
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public StockReservation Reservation { get; set; } = default!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
}
