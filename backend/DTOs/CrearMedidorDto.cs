using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public class CrearMedidorDto
{
    [Required, StringLength(50)]
    public string NumeroSerie { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string NumeroSuministro { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string TipoMedicion { get; set; } = string.Empty;

    public DateTime FechaInstalacion { get; set; }
}
