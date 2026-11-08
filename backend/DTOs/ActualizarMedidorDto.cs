using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class ActualizarMedidorDto : CrearMedidorDto
{
    [Required, StringLength(250, MinimumLength = 5)]
    public string Motivo { get; set; } = string.Empty;
}
