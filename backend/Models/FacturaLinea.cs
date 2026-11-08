namespace SistemaElectrico.Api.Models;

public class FacturaLinea
{
    public int Id { get; set; }

    public int FacturaId { get; set; }

    public Factura Factura { get; set; } = null!;

    public int NumeroLinea { get; set; }

    public string CodigoOriginal { get; set; } = string.Empty;

    public string DescripcionOriginal { get; set; } = string.Empty;

    public string Categoria { get; set; } = "OTRO";

    public decimal Monto { get; set; }

    public bool EsIngresoElectrico { get; set; }
}
