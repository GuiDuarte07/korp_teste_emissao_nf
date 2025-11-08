using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.Inventory
{
    public record CreateProductRequest
    {
        [Required(ErrorMessage = "O código do produto é obrigatório.")]
        [StringLength(50, ErrorMessage = "O código deve ter no máximo 50 caracteres.")]
        public string Code { get; init; } = default!;

        [Required(ErrorMessage = "A descrição do produto é obrigatória.")]
        [StringLength(150, ErrorMessage = "A descrição deve ter no máximo 150 caracteres.")]
        public string Description { get; init; } = default!;

        [Range(0, int.MaxValue, ErrorMessage = "O estoque inicial deve ser maior ou igual a zero.")]
        public int InitialStock { get; init; }
    }
}
