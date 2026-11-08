namespace SistemaElectrico.Api.DTOs;

public sealed class ClienteDetalleDto
{
    public int Id { get; set; }
    public string TipoIdentificacion { get; set; } = string.Empty;
    public string Identificacion { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? DireccionNotificacion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
}
