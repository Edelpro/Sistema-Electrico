using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class CrearOrdenTrabajoDto
{
    [Required, MaxLength(30)] public string NumeroSuministro { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Tipo { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Causa { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Prioridad { get; set; } = "NORMAL";
    public DateTime? FechaLimiteSlaUtc { get; set; }
    [EmailAddress, MaxLength(160)] public string? CorreoTecnico { get; set; }
    [MaxLength(30)] public string? CodigoCuadrilla { get; set; }
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
}

public sealed class AsignarOrdenDto
{
    [EmailAddress, MaxLength(160)] public string? CorreoTecnico { get; set; }
    [MaxLength(30)] public string? CodigoCuadrilla { get; set; }
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
}

public sealed class TransicionarOrdenDto
{
    [Required, MaxLength(30)] public string EstadoNuevo { get; set; } = string.Empty;
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
    [MaxLength(500)] public string? Observacion { get; set; }
}

public sealed record OrdenHistorialDto(string? EstadoAnterior, string EstadoNuevo, DateTime FechaUtc,
    string Motivo, string? Actor);
public sealed record OrdenTrabajoResumenDto(int Id, string Codigo, string NumeroSuministro, string TitularActual,
    string Zona, string Tipo, string Causa, string Estado, string Prioridad, DateTime? FechaLimiteSlaUtc,
    DateTime FechaCreacionUtc, string? Tecnico, string? Cuadrilla);
public sealed record OrdenTrabajoDetalleDto(int Id, string Codigo, string NumeroSuministro, string TitularActual,
    string DireccionServicio, string Zona, string Tipo, string Causa, string Estado, string Prioridad,
    DateTime? FechaLimiteSlaUtc, DateTime FechaCreacionUtc, DateTime FechaActualizacionUtc, bool Activa,
    string? Tecnico, string? Cuadrilla, string? Observacion, int? PagoId, IReadOnlyList<OrdenHistorialDto> Historial);
public sealed record OrdenesPaginaDto(IReadOnlyList<OrdenTrabajoResumenDto> Items, int Total, int Pagina, int TamanoPagina);
