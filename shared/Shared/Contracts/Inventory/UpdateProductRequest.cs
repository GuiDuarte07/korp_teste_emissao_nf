using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.Inventory
{
    public class UpdateProductRequest
    {
        public Guid Id { get; set; }

        [StringLength(50, ErrorMessage = "O código deve ter no máximo 50 caracteres.")]
        public string Code { get; init; } = default!;

        [StringLength(150, ErrorMessage = "A descrição deve ter no máximo 150 caracteres.")]
        public string? Description { get; init; }

        [Range(0, int.MaxValue, ErrorMessage = "O estoque deve ser maior ou igual a zero.")]
        public int? Stock { get; init; }
    }
}
