namespace InvoiceService.Domain.Entities;

/// <summary>
/// Armazena chaves de idempotência para evitar duplicação de requisições
/// </summary>
public class IdempotencyKey
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Chave única fornecida pelo cliente (ex: UUID gerado no frontend)
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// ID da nota fiscal criada com esta key
    /// </summary>
    public Guid InvoiceId { get; set; }
    
    /// <summary>
    /// Timestamp da primeira requisição
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Response serializado (JSON) para retornar em requisições duplicadas
    /// </summary>
    public string? ResponsePayload { get; set; }
    
    /// <summary>
    /// Data de expiração (após 24h, pode ser removido)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
