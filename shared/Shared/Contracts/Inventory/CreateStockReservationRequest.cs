using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.Inventory
{
    public record CreateStockReservationRequest
    {
        [Required(ErrorMessage = "O identificador da nota fiscal é obrigatório.")]
        public Guid InvoiceId { get; init; }

        [Required(ErrorMessage = "É necessário informar os produtos da reserva.")]
        [MinLength(1, ErrorMessage = "A reserva deve conter pelo menos um item.")]
        public List<CreateStockReservationItemRequest> Items { get; init; } = new();
    }

    public record CreateStockReservationItemRequest
    {
        [Required(ErrorMessage = "O identificador do produto é obrigatório.")]
        public Guid ProductId { get; init; }

        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantity { get; init; }
    }
}
