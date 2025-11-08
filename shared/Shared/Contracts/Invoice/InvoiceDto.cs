namespace Shared.Contracts.Invoice;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public int InvoiceNumber { get; set; }
    public string Status { get; set; } = string.Empty; // "Open" ou "Closed"
    public DateTime CreatedAt { get; set; }
    public DateTime? PrintedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
