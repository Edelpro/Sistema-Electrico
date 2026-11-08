namespace SistemaElectrico.Api.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public string Identificacion { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? UltimoAcceso { get; set; }

    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public DateTime? CredencialesActualizadasUtc { get; set; }

    public int RolId { get; set; }

    public Rol Rol { get; set; } = null!;

    public int? ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    public ICollection<EvidenciaOrden> EvidenciasTecnicas { get; set; } = [];

    public ICollection<TokenRecuperacionPassword> TokensRecuperacion { get; set; } = [];
}
