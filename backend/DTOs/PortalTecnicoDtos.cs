using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed record PortalTecnicoResumenDto(
    int Total,
    int Pendientes,
    int EnCurso,
    int Criticas,
    IReadOnlyList<TecnicoOrdenResumenDto> Ordenes);

public sealed record TecnicoOrdenResumenDto(
    int Id,
    string Codigo,
    string NumeroSuministro,
    string DireccionServicio,
    string Zona,
    string Tipo,
    string Causa,
    string Estado,
    string Prioridad,
    DateTime? FechaLimiteSlaUtc,
    DateTime FechaActualizacionUtc,
    bool TieneEvidencia);

public sealed record TecnicoOrdenDetalleDto(
    TecnicoOrdenResumenDto Orden,
    string Titular,
    string? Observacion,
    IReadOnlyList<string> TransicionesPermitidas,
    IReadOnlyList<OrdenHistorialDto> Historial,
    IReadOnlyList<EvidenciaTecnicaDto> Evidencias);

public sealed record EvidenciaTecnicaDto(
    long Id,
    DateTime FechaDispositivoUtc,
    DateTime FechaServidorUtc,
    decimal? PrecisionMetros,
    decimal? LecturaMedidor,
    string Observacion,
    string NombreArchivo);

public sealed class TransicionTecnicoDto
{
    [Required, MaxLength(30)] public string EstadoNuevo { get; set; } = string.Empty;
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
    [MaxLength(500)] public string? Observacion { get; set; }
}

public sealed class RegistrarEvidenciaDto
{
    [Required] public IFormFile? Fotografia { get; set; }
    [Required] public DateTime FechaDispositivoUtc { get; set; }
    [Range(-90, 90)] public decimal? Latitud { get; set; }
    [Range(-180, 180)] public decimal? Longitud { get; set; }
    [Range(0, 10000)] public decimal? PrecisionMetros { get; set; }
    [Range(0, 999999999)] public decimal? LecturaMedidor { get; set; }
    [Required, MinLength(5), MaxLength(1000)] public string Observacion { get; set; } = string.Empty;
}
