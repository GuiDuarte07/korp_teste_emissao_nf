namespace Shared.Contracts.Inventory
{
    public record HealthCheckResponse
    {
        public string Status { get; init; } = default!;
        public bool DatabaseConnected { get; init; }
        public bool QueueConnected { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
