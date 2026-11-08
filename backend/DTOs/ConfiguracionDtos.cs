using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed record ConfiguracionSistemaDto(int Id, string NombreEmpresa, string Ruc,
    string CorreoSoporte, string TelefonoSoporte, string Direccion, string ZonaHoraria,
    int TamanoMaximoEvidenciaMb, bool NotificacionesCorreoActivas,
    bool NotificacionesWhatsAppActivas, DateTime FechaActualizacionUtc);

public sealed record PoliticaSlaDto(int Id, int Version, string TipoOrden, string TipoZona,
    int HorasObjetivo, string HoraInicioLaboral, string HoraFinLaboral,
    bool IncluyeFinDeSemana, DateTime VigenteDesdeUtc, DateTime? VigenteHastaUtc,
    bool Activa, string Motivo);

public sealed record ConfiguracionCompletaDto(ConfiguracionSistemaDto General,
    IReadOnlyCollection<PoliticaSlaDto> PoliticasSla);

public sealed class ActualizarConfiguracionDto
{
    [Required, StringLength(160)] public string NombreEmpresa { get; set; } = string.Empty;
    [StringLength(20)] public string Ruc { get; set; } = string.Empty;
    [EmailAddress, StringLength(160)] public string CorreoSoporte { get; set; } = string.Empty;
    [StringLength(30)] public string TelefonoSoporte { get; set; } = string.Empty;
    [StringLength(300)] public string Direccion { get; set; } = string.Empty;
    [Required, StringLength(80)] public string ZonaHoraria { get; set; } = "America/Guayaquil";
    [Range(1, 20)] public int TamanoMaximoEvidenciaMb { get; set; } = 5;
    public bool NotificacionesCorreoActivas { get; set; }
    public bool NotificacionesWhatsAppActivas { get; set; }
    [Required, MinLength(5), StringLength(300)] public string Motivo { get; set; } = string.Empty;
}

public sealed class CrearPoliticaSlaDto
{
    [Required, RegularExpression("^(RECONEXION|CORTE)$")] public string TipoOrden { get; set; } = "RECONEXION";
    [Required, RegularExpression("^(URBANA|RURAL|TODAS)$")] public string TipoZona { get; set; } = "URBANA";
    [Range(1, 168)] public int HorasObjetivo { get; set; } = 10;
    [Required] public string HoraInicioLaboral { get; set; } = "08:00";
    [Required] public string HoraFinLaboral { get; set; } = "17:00";
    public bool IncluyeFinDeSemana { get; set; }
    [Required, MinLength(5), StringLength(300)] public string Motivo { get; set; } = string.Empty;
}

public sealed class CambiarEstadoPoliticaSlaDto
{
    public bool Activa { get; set; }
    [Required, MinLength(5), StringLength(300)] public string Motivo { get; set; } = string.Empty;
}
