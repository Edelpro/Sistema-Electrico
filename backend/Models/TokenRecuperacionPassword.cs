namespace SistemaElectrico.Api.Models;

public sealed class TokenRecuperacionPassword
{
    public long Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreadoEnUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEnUtc { get; set; }
    public DateTime? UsadoEnUtc { get; set; }
}
