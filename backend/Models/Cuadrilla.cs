namespace SistemaElectrico.Api.Models;

public class Cuadrilla
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Zona { get; set; } = string.Empty;
    public int CapacidadMaxima { get; set; } = 1;
    public string Estado { get; set; } = "DISPONIBLE";
    public bool Activa { get; set; } = true;
    public string? Observaciones { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacionUtc { get; set; } = DateTime.UtcNow;
    public ICollection<CuadrillaMiembro> Miembros { get; set; } = [];
    public ICollection<OrdenTrabajo> Ordenes { get; set; } = [];
}
