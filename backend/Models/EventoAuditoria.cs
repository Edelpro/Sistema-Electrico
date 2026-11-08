namespace SistemaElectrico.Api.Models;

public class EventoAuditoria
{
    public long Id { get; set; }

    public int? ActorUsuarioId { get; set; }

    public Usuario? ActorUsuario { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string TipoEntidad { get; set; } = string.Empty;

    public string EntidadId { get; set; } = string.Empty;

    public string? ValorAnteriorJson { get; set; }

    public string? ValorNuevoJson { get; set; }

    public DateTime OcurridoEnUtc { get; set; } = DateTime.UtcNow;

    public string? Motivo { get; set; }
}
