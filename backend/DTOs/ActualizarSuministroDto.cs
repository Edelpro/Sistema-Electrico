using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class ActualizarSuministroDto : CrearSuministroDto
{
    [Required, StringLength(250, MinimumLength = 5)]
    public string Motivo { get; set; } = string.Empty;
}
