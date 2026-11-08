namespace SistemaElectrico.Api.DTOs;

public sealed class FacturaDetalleDto : FacturaResumenDto
{
    public string Cuen { get; set; } = string.Empty;
    public string? NumeroMedidor { get; set; }
    public string SistemaOrigen { get; set; } = string.Empty;
    public string IdentificadorExterno { get; set; } = string.Empty;
    public string NombreEmisor { get; set; } = string.Empty;
    public string RucEmisor { get; set; } = string.Empty;
    public string Moneda { get; set; } = "USD";
    public string MetodoLectura { get; set; } = string.Empty;
    public decimal LecturaAnterior { get; set; }
    public decimal LecturaActual { get; set; }
    public string UnidadConsumo { get; set; } = "kWh";
    public decimal SubtotalServicioElectrico { get; set; }
    public decimal SubtotalAlumbradoPublico { get; set; }
    public decimal OtrosCargos { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal RecaudacionTerceros { get; set; }
    public decimal Subsidios { get; set; }
    public DateTime SincronizadaEnUtc { get; set; }
    public List<FacturaLineaDto> Lineas { get; set; } = [];
}

public sealed class FacturaLineaDto
{
    public int NumeroLinea { get; set; }
    public string CodigoOriginal { get; set; } = string.Empty;
    public string DescripcionOriginal { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public bool EsIngresoElectrico { get; set; }
}
