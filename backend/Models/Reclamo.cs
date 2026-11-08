namespace SistemaElectrico.Api.Models;

public class Reclamo
{
    public int Id { get; set; }
    public string NumeroCaso { get; set; } = string.Empty;
    public int SuministroId { get; set; }
    public Suministro Suministro { get; set; } = null!;
    public int? PagoId { get; set; }
    public Pago? Pago { get; set; }
    public int? OrdenTrabajoId { get; set; }
    public OrdenTrabajo? OrdenTrabajo { get; set; }
    public int? CreadoPorUsuarioId { get; set; }
    public Usuario? CreadoPorUsuario { get; set; }
    public int? AsignadoAUsuarioId { get; set; }
    public Usuario? AsignadoAUsuario { get; set; }
    public string Tipo { get; set; } = "OTRO";
    public string Estado { get; set; } = "ABIERTO";
    public string Prioridad { get; set; } = "NORMAL";
    public string Descripcion { get; set; } = string.Empty;
    public string? Resolucion { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierreUtc { get; set; }
    public ICollection<ReclamoHistorial> Historial { get; set; } = [];
}
