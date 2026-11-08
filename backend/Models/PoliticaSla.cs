namespace SistemaElectrico.Api.Models;

public class PoliticaSla
{
    public int Id { get; set; }
    public int Version { get; set; } = 1;
    public string TipoOrden { get; set; } = "RECONEXION";
    public string TipoZona { get; set; } = "URBANA";
    public int HorasObjetivo { get; set; } = 10;
    public TimeSpan HoraInicioLaboral { get; set; } = new(8, 0, 0);
    public TimeSpan HoraFinLaboral { get; set; } = new(17, 0, 0);
    public bool IncluyeFinDeSemana { get; set; }
    public DateTime VigenteDesdeUtc { get; set; } = DateTime.UtcNow;
    public DateTime? VigenteHastaUtc { get; set; }
    public bool Activa { get; set; } = true;
    public string Motivo { get; set; } = string.Empty;
    public int? CreadoPorUsuarioId { get; set; }
    public Usuario? CreadoPorUsuario { get; set; }
}
