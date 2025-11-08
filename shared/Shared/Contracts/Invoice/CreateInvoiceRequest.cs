using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts.Invoice;

public class CreateInvoiceRequest
{
    [Required(ErrorMessage = "Pelo menos um item é obrigatório")]
    [MinLength(1, ErrorMessage = "A nota fiscal deve ter pelo menos um item")]
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}

public class CreateInvoiceItemRequest
{
    [Required(ErrorMessage = "O ID do produto é obrigatório")]
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "A quantidade é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero")]
    public int Quantity { get; set; }
}
