namespace Shared.Contracts.Inventory
{
    public record StockReservationResponse
    {
        public Guid Id { get; init; }
        public Guid InvoiceId { get; init; }
        public string Status { get; init; } = default!;
        public List<StockReservationItemResponse> Items { get; init; } = new();
        public DateTime CreatedAt { get; init; }
    }

    public record StockReservationItemResponse
    {
        public Guid ProductId { get; init; }
        public string ProductCode { get; init; } = default!;
        public string ProductDescription { get; init; } = default!;
        public int Quantity { get; init; }
    }
}
