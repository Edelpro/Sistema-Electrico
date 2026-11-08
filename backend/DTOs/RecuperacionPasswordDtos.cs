using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class SolicitarRecuperacionPasswordDto
{
    [Required, EmailAddress, StringLength(160)]
    public string Correo { get; set; } = string.Empty;
}

public sealed class RestablecerPasswordDto
{
    [Required, EmailAddress, StringLength(160)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(10), StringLength(128)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$",
        ErrorMessage = "La contraseña debe incluir mayúscula, minúscula, número y símbolo.")]
    public string NuevaPassword { get; set; } = string.Empty;
}
