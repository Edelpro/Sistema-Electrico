namespace SistemaElectrico.Api.Models;

public class Medidor
{
    public int Id { get; set; }

    public string NumeroSerie { get; set; } = string.Empty;

    public string TipoMedicion { get; set; } = "CONVENCIONAL";

    public string Estado { get; set; } = "ACTIVO";

    public DateTime FechaInstalacion { get; set; }

    public DateTime? FechaRetiro { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public int SuministroId { get; set; }

    public Suministro Suministro { get; set; } = null!;

    public ICollection<Factura> Facturas { get; set; } = [];
}
