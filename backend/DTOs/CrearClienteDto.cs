using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class CrearClienteDto
{
    [Required]
    [StringLength(20)]
    public string TipoIdentificacion { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Identificacion { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Apellidos { get; set; }

    [EmailAddress]
    [StringLength(160)]
    public string? Correo { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    [StringLength(300)]
    public string? DireccionNotificacion { get; set; }
}
