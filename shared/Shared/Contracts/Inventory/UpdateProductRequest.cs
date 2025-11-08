using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.Inventory
{
    public class UpdateProductRequest
    {
        public Guid Id { get; set; }

        [StringLength(50, MinimumLength = 3, ErrorMessage = "O código deve ter entre 3 e 50 caracteres.")]
        public string? Code { get; init; } = default!;

        [StringLength(150, MinimumLength = 5, ErrorMessage = "A descrição deve ter entre 5 e 150 caracteres.")]
        public string? Description { get; init; }

        [Range(0, int.MaxValue, ErrorMessage = "O estoque deve ser maior ou igual a zero.")]
        public int? Stock { get; init; }
    }
}
