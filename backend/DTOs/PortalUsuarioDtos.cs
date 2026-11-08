using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed record PortalUsuarioResumenDto(
    string NombreCompleto,
    string Cliente,
    IReadOnlyList<PortalSuministroResumenDto> Suministros);

public sealed record PortalSuministroResumenDto(
    int Id,
    string NumeroSuministro,
    string NumeroContrato,
    string DireccionServicio,
    string EstadoServicio,
    decimal SaldoPendiente,
    int FacturasPendientes,
    string? EstadoOrdenActiva,
    DateTime? FechaLimiteSlaUtc,
    int ReclamosAbiertos);

public sealed record PortalSuministroDetalleDto(
    PortalSuministroResumenDto Suministro,
    IReadOnlyList<PortalFacturaDto> Facturas,
    IReadOnlyList<PortalPagoDto> Pagos,
    IReadOnlyList<PortalOrdenDto> Ordenes,
    IReadOnlyList<PortalReclamoDto> Reclamos,
    IReadOnlyList<PortalEventoDto> LineaTiempo);

public sealed record PortalFacturaDto(
    int Id,
    string NumeroFactura,
    DateTime FechaEmision,
    DateTime FechaVencimiento,
    DateTime PeriodoDesde,
    DateTime PeriodoHasta,
    decimal ConsumoFacturado,
    string UnidadConsumo,
    string Estado,
    decimal TotalFactura,
    decimal SaldoPendiente);

public sealed record PortalPagoDto(
    int Id,
    string Referencia,
    decimal Monto,
    string Moneda,
    string Estado,
    DateTime FechaConfirmacionUtc);

public sealed record PortalOrdenDto(
    int Id,
    string Codigo,
    string Tipo,
    string Estado,
    string Prioridad,
    DateTime FechaCreacionUtc,
    DateTime? FechaLimiteSlaUtc,
    DateTime? EvidenciaFechaUtc,
    long? EvidenciaId);

public sealed record PortalReclamoDto(
    int Id,
    string NumeroCaso,
    string Tipo,
    string Estado,
    string Prioridad,
    string Descripcion,
    string? Resolucion,
    DateTime FechaCreacionUtc,
    DateTime? FechaCierreUtc);

public sealed record PortalEventoDto(
    string Tipo,
    string Titulo,
    string Descripcion,
    DateTime FechaUtc,
    string Estado);

public sealed class CrearReclamoUsuarioDto
{
    [Required, MaxLength(30)] public string NumeroSuministro { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string Tipo { get; set; } = string.Empty;
    [Required, MinLength(10), MaxLength(1200)] public string Descripcion { get; set; } = string.Empty;
    public int? PagoId { get; set; }
    public int? OrdenTrabajoId { get; set; }
}
