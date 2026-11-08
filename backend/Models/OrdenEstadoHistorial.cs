namespace SistemaElectrico.Api.Models;

public class OrdenEstadoHistorial
{
    public long Id { get; set; }
    public int OrdenTrabajoId { get; set; }
    public OrdenTrabajo OrdenTrabajo { get; set; } = null!;
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public int? ActorUsuarioId { get; set; }
    public Usuario? ActorUsuario { get; set; }
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public string Motivo { get; set; } = string.Empty;
}
