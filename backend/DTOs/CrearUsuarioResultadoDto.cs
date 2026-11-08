namespace SistemaElectrico.Api.DTOs;

public sealed class CrearUsuarioResultadoDto
{
    public UsuarioDetalleDto Usuario { get; set; } = null!;
    public bool InvitacionEnviada { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

public sealed class EnvioInvitacionUsuarioDto
{
    public bool InvitacionEnviada { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
