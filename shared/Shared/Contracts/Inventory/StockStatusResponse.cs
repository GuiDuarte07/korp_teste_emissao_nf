namespace Shared.Contracts.Inventory
{
    public record StockStatusResponse
    {
        public int TotalProducts { get; init; }
        public int TotalStock { get; init; }
        public int TotalReserved { get; init; }
        public int TotalAvailable { get; init; }
    }
}
