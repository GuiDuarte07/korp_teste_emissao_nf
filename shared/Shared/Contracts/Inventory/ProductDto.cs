namespace Shared.Contracts.Inventory
{
    public record ProductDto
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = default!;
        public string Description { get; init; } = default!;
        public int Stock { get; init; }
        public int ReservedStock { get; init; }
        public int AvailableStock => Stock - ReservedStock;
    }
}
