namespace SistemaElectrico.Api.DTOs;

public class FacturaResumenDto
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public string NumeroSuministro { get; set; } = string.Empty;
    public string NumeroContrato { get; set; } = string.Empty;
    public string TitularActual { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public decimal ConsumoFacturado { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal TotalFactura { get; set; }
    public decimal SaldoPendiente { get; set; }
    public bool TieneDiferencia { get; set; }
}
