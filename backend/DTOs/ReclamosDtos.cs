using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class CrearReclamoDto
{
    [Required, MaxLength(30)] public string NumeroSuministro { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string Tipo { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Prioridad { get; set; } = "NORMAL";
    [Required, MinLength(10), MaxLength(1200)] public string Descripcion { get; set; } = string.Empty;
    public int? PagoId { get; set; }
    public int? OrdenTrabajoId { get; set; }
    [EmailAddress, MaxLength(160)] public string? CorreoAsignado { get; set; }
}

public sealed class GestionarReclamoDto
{
    [Required, MaxLength(30)] public string EstadoNuevo { get; set; } = string.Empty;
    [Required, MinLength(5), MaxLength(500)] public string Motivo { get; set; } = string.Empty;
    [MaxLength(1200)] public string? Resolucion { get; set; }
    [EmailAddress, MaxLength(160)] public string? CorreoAsignado { get; set; }
}

public sealed record ReclamoHistorialDto(string? EstadoAnterior, string EstadoNuevo, string Motivo,
    DateTime FechaUtc, string? Actor);
public sealed record ReclamoResumenDto(int Id, string NumeroCaso, string NumeroSuministro, string TitularActual,
    string Tipo, string Estado, string Prioridad, DateTime FechaCreacionUtc, string? AsignadoA);
public sealed record ReclamoDetalleDto(int Id, string NumeroCaso, string NumeroSuministro, string TitularActual,
    string DireccionServicio, string Tipo, string Estado, string Prioridad, string Descripcion, string? Resolucion,
    int? PagoId, int? OrdenTrabajoId, string? AsignadoA, DateTime FechaCreacionUtc, DateTime FechaActualizacionUtc,
    DateTime? FechaCierreUtc, IReadOnlyList<ReclamoHistorialDto> Historial);
public sealed record ReclamosPaginaDto(IReadOnlyList<ReclamoResumenDto> Items, int Total, int Pagina, int TamanoPagina);
