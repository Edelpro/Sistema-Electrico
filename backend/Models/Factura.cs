namespace SistemaElectrico.Api.Models;

public class Factura
{
    public int Id { get; set; }

    public string SistemaOrigen { get; set; } = string.Empty;

    public string IdentificadorExterno { get; set; } = string.Empty;

    public string NumeroFactura { get; set; } = string.Empty;

    public string NombreEmisor { get; set; } = string.Empty;

    public string RucEmisor { get; set; } = string.Empty;

    public DateTime FechaEmision { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public string Moneda { get; set; } = "USD";

    public string Estado { get; set; } = "EMITIDA";

    public DateTime PeriodoDesde { get; set; }

    public DateTime PeriodoHasta { get; set; }

    public string MetodoLectura { get; set; } = "REAL";

    public decimal LecturaAnterior { get; set; }

    public decimal LecturaActual { get; set; }

    public decimal ConsumoFacturado { get; set; }

    public string UnidadConsumo { get; set; } = "kWh";

    public decimal SubtotalServicioElectrico { get; set; }

    public decimal SubtotalAlumbradoPublico { get; set; }

    public decimal OtrosCargos { get; set; }

    public decimal SaldoAnterior { get; set; }

    public decimal RecaudacionTerceros { get; set; }

    public decimal Subsidios { get; set; }

    public decimal TotalFactura { get; set; }

    public decimal SaldoPendiente { get; set; }

    public bool TieneDiferencia { get; set; }

    public DateTime SincronizadaEnUtc { get; set; } = DateTime.UtcNow;

    public int SuministroId { get; set; }

    public Suministro Suministro { get; set; } = null!;

    public int? MedidorId { get; set; }

    public Medidor? Medidor { get; set; }

    public ICollection<FacturaLinea> Lineas { get; set; } = [];

    public ICollection<PagoAsignacion> AsignacionesPago { get; set; } = [];
}
