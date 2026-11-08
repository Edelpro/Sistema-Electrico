namespace SistemaElectrico.Api.DTOs;

public sealed class UsuarioResumenDto
{
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string Identificacion { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string Rol { get; set; } = string.Empty;

    public string? ClienteVinculado { get; set; }

    public bool Activo { get; set; }

    public DateTime? UltimoAcceso { get; set; }
}
