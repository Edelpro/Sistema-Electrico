namespace SistemaElectrico.Api.Models;

public class ReclamoHistorial
{
    public long Id { get; set; }
    public int ReclamoId { get; set; }
    public Reclamo Reclamo { get; set; } = null!;
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public int? ActorUsuarioId { get; set; }
    public Usuario? ActorUsuario { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}
