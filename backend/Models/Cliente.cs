namespace SistemaElectrico.Api.Models;

public class Cliente
{
    public int Id { get; set; }

    public string TipoIdentificacion { get; set; } = string.Empty;

    public string Identificacion { get; set; } = string.Empty;

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? DireccionNotificacion { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public ICollection<Suministro> SuministrosTitulares { get; set; } = [];

    public Usuario? UsuarioPortal { get; set; }
}
