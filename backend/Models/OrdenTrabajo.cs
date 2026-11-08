namespace SistemaElectrico.Api.Models;

public class OrdenTrabajo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int SuministroId { get; set; }
    public Suministro Suministro { get; set; } = null!;
    public int? PagoId { get; set; }
    public Pago? Pago { get; set; }
    public int? TecnicoUsuarioId { get; set; }
    public Usuario? TecnicoUsuario { get; set; }
    public int? CuadrillaId { get; set; }
    public Cuadrilla? Cuadrilla { get; set; }
    public string Tipo { get; set; } = "RECONEXION";
    public string Causa { get; set; } = string.Empty;
    public string Estado { get; set; } = "CREADA";
    public string Prioridad { get; set; } = "NORMAL";
    public DateTime? FechaLimiteSlaUtc { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacionUtc { get; set; } = DateTime.UtcNow;
    public bool Activa { get; set; } = true;
    public string? Observacion { get; set; }
    public ICollection<OrdenEstadoHistorial> Historial { get; set; } = [];
    public ICollection<EvidenciaOrden> Evidencias { get; set; } = [];
    public ICollection<Reclamo> Reclamos { get; set; } = [];
}
