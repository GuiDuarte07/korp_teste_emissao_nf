namespace InvoiceService.Domain.Entities;

public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    
    // Snapshot dos dados do produto (para compliance e histórico)
    public Guid ProductId { get; set; } // Referência para rastreabilidade
    public string ProductCode { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    
    // Link com o Inventory Service
    public Guid? ReservationId { get; set; } // ID da reserva no InventoryService
    
    public Invoice Invoice { get; set; } = null!;
}
