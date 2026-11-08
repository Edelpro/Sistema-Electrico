namespace SistemaElectrico.Api.Models;

public sealed class EvidenciaOrden
{
    public long Id { get; set; }
    public int OrdenTrabajoId { get; set; }
    public OrdenTrabajo OrdenTrabajo { get; set; } = null!;
    public int TecnicoUsuarioId { get; set; }
    public Usuario TecnicoUsuario { get; set; } = null!;
    public DateTime FechaDispositivoUtc { get; set; }
    public DateTime FechaServidorUtc { get; set; } = DateTime.UtcNow;
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public decimal? PrecisionMetros { get; set; }
    public decimal? LecturaMedidor { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public string ArchivoAlmacenado { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
}
