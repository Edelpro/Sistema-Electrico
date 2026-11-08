namespace SistemaElectrico.Api.Models;

public class Suministro
{
    public int Id { get; set; }

    public string NumeroSuministro { get; set; } = string.Empty;

    public string NumeroContrato { get; set; } = string.Empty;

    public string Cuen { get; set; } = string.Empty;

    public string DireccionServicio { get; set; } = string.Empty;

    public string? Provincia { get; set; }

    public string? Canton { get; set; }

    public string? Parroquia { get; set; }

    public string? PlanGeocodigo { get; set; }

    public string? CodigoTarifa { get; set; }

    public string? NombreTarifa { get; set; }

    public string EstadoServicio { get; set; } = "ACTIVO";

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public int ClienteTitularId { get; set; }

    public Cliente ClienteTitular { get; set; } = null!;

    public ICollection<Medidor> Medidores { get; set; } = [];

    public ICollection<Factura> Facturas { get; set; } = [];

    public ICollection<Pago> Pagos { get; set; } = [];

    public ICollection<OrdenTrabajo> OrdenesTrabajo { get; set; } = [];

    public ICollection<Reclamo> Reclamos { get; set; } = [];
}
