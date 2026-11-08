using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class CrearUsuarioDto
{
    [Required]
    [StringLength(100)]
    public string Nombres { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Apellidos { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Identificacion { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(160)]
    public string Correo { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required]
    [StringLength(20)]
    public string Rol { get; set; } = string.Empty;

    [StringLength(20)]
    public string? IdentificacionCliente { get; set; }

    public bool Activo { get; set; } = true;

    public bool EnviarInvitacion { get; set; } = true;
}
