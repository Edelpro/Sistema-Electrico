namespace SistemaElectrico.Api.Models;

public class Pago
{
    public int Id { get; set; }
    public int SuministroId { get; set; }
    public Suministro Suministro { get; set; } = null!;
    public string Proveedor { get; set; } = string.Empty;
    public string EventoProveedorId { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "USD";
    public string Estado { get; set; } = "CONFIRMADO";
    public DateTime FechaProveedorUtc { get; set; }
    public DateTime FechaRecepcionUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaConfirmacionUtc { get; set; } = DateTime.UtcNow;
    public string MotivoConciliacion { get; set; } = string.Empty;
    public ICollection<PagoAsignacion> Asignaciones { get; set; } = [];
    public ICollection<OrdenTrabajo> OrdenesGeneradas { get; set; } = [];
    public ICollection<Reclamo> Reclamos { get; set; } = [];
}
