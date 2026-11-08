using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class CambiarEstadoMedidorDto
{
    [Required, StringLength(30)]
    public string Estado { get; set; } = string.Empty;

    [Required, StringLength(250, MinimumLength = 5)]
    public string Motivo { get; set; } = string.Empty;
}
