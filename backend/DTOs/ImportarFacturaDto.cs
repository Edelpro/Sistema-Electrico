namespace SistemaElectrico.Api.DTOs;

public sealed class ImportarFacturaDto
{
    public string NumeroSuministro { get; set; } = string.Empty;
    public string? NumeroMedidor { get; set; }
    public string SistemaOrigen { get; set; } = string.Empty;
    public string IdentificadorExterno { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public string NombreEmisor { get; set; } = string.Empty;
    public string RucEmisor { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Estado { get; set; } = "EMITIDA";
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public string MetodoLectura { get; set; } = "REAL";
    public decimal LecturaAnterior { get; set; }
    public decimal LecturaActual { get; set; }
    public decimal ConsumoFacturado { get; set; }
    public decimal SubtotalServicioElectrico { get; set; }
    public decimal SubtotalAlumbradoPublico { get; set; }
    public decimal OtrosCargos { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal RecaudacionTerceros { get; set; }
    public decimal Subsidios { get; set; }
    public decimal TotalFactura { get; set; }
    public decimal SaldoPendiente { get; set; }
    public List<ImportarFacturaLineaDto> Lineas { get; set; } = [];
}

public sealed class ImportarFacturaLineaDto
{
    public string CodigoOriginal { get; set; } = string.Empty;
    public string DescripcionOriginal { get; set; } = string.Empty;
    public string Categoria { get; set; } = "OTRO";
    public decimal Monto { get; set; }
    public bool EsIngresoElectrico { get; set; }
}
