namespace InventoryService.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navegação
    public ICollection<StockReservationItem> ReservationItems { get; set; } = new List<StockReservationItem>();
}
