namespace InvoiceService.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public int InvoiceNumber { get; set; } // Numeração sequencial
    public InvoiceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PrintedAt { get; set; } // Data de impressão (quando virou Closed)
    public bool Cancelled { get; set; } // Nota fiscal cancelada (soft delete)
    public DateTime? CancelledAt { get; set; } // Data de cancelamento
    
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}

public enum InvoiceStatus
{
    Open = 0,    // Aberta - pode ser impressa
    Closed = 1   // Fechada - já foi impressa, estoque debitado
}
