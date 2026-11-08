namespace SistemaElectrico.Api.Models;

public class ConfiguracionSistema
{
    public int Id { get; set; }
    public string NombreEmpresa { get; set; } = "Empresa Eléctrica";
    public string Ruc { get; set; } = string.Empty;
    public string CorreoSoporte { get; set; } = string.Empty;
    public string TelefonoSoporte { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string ZonaHoraria { get; set; } = "America/Guayaquil";
    public int TamanoMaximoEvidenciaMb { get; set; } = 5;
    public bool NotificacionesCorreoActivas { get; set; }
    public bool NotificacionesWhatsAppActivas { get; set; }
    public DateTime FechaActualizacionUtc { get; set; } = DateTime.UtcNow;
    public int? ActualizadoPorUsuarioId { get; set; }
    public Usuario? ActualizadoPorUsuario { get; set; }
}
