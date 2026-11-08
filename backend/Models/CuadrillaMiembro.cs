namespace SistemaElectrico.Api.Models;

public class CuadrillaMiembro
{
    public int Id { get; set; }
    public int CuadrillaId { get; set; }
    public Cuadrilla Cuadrilla { get; set; } = null!;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public DateTime FechaAsignacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFinUtc { get; set; }
}
