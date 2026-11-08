namespace SistemaElectrico.Api.Models;

public class PagoAsignacion
{
    public int Id { get; set; }
    public int PagoId { get; set; }
    public Pago Pago { get; set; } = null!;
    public int FacturaId { get; set; }
    public Factura Factura { get; set; } = null!;
    public decimal MontoAplicado { get; set; }
    public DateTime AplicadoEnUtc { get; set; } = DateTime.UtcNow;
}
