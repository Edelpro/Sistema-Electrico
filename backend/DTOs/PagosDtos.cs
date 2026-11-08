using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class ConciliarPagoDto
{
    [Required, MaxLength(30)] public string NumeroSuministro { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Proveedor { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string EventoProveedorId { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Referencia { get; set; } = string.Empty;
    [Range(0.01, 999999999999)] public decimal Monto { get; set; }
    public DateTime FechaProveedorUtc { get; set; }
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
    [MinLength(1)] public List<AsignarPagoDto> Asignaciones { get; set; } = [];
}

public sealed class AsignarPagoDto
{
    [Required, MaxLength(60)] public string NumeroFactura { get; set; } = string.Empty;
    [Range(0.01, 999999999999)] public decimal MontoAplicado { get; set; }
}

public sealed record PagoAsignacionDto(int FacturaId, string NumeroFactura, decimal MontoAplicado);
public sealed record ResultadoOperativoPagoDto(string Tipo, string Mensaje, int? OrdenId, string? CodigoOrden);
public sealed record PagoResumenDto(int Id, string NumeroSuministro, string TitularActual, string Proveedor,
    string Referencia, decimal Monto, string Estado, DateTime FechaConfirmacionUtc, int FacturasAplicadas);
public sealed record PagoDetalleDto(int Id, string NumeroSuministro, string TitularActual, string Proveedor,
    string EventoProveedorId, string Referencia, decimal Monto, string Moneda, string Estado,
    DateTime FechaProveedorUtc, DateTime FechaRecepcionUtc, DateTime FechaConfirmacionUtc,
    string MotivoConciliacion, IReadOnlyList<PagoAsignacionDto> Asignaciones);
public sealed record PagosPaginaDto(IReadOnlyList<PagoResumenDto> Items, int Total, int Pagina, int TamanoPagina);
public sealed record ResultadoConciliacionPagoDto(PagoDetalleDto Pago, bool YaExistia, ResultadoOperativoPagoDto ResultadoOperativo);
