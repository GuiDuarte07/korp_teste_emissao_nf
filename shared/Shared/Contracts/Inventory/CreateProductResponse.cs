namespace Shared.Contracts.Inventory
{
    public record CreateProductResponse
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = default!;
        public string Description { get; init; } = default!;
        public int Stock { get; init; }
    }
}
