using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public class CrearSuministroDto
{
    [Required, StringLength(30)]
    public string NumeroSuministro { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string NumeroContrato { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Cuen { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string ClienteIdentificacion { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string DireccionServicio { get; set; } = string.Empty;

    [StringLength(80)] public string? Provincia { get; set; }
    [StringLength(80)] public string? Canton { get; set; }
    [StringLength(80)] public string? Parroquia { get; set; }
    [StringLength(50)] public string? PlanGeocodigo { get; set; }
    [StringLength(40)] public string? CodigoTarifa { get; set; }
    [StringLength(100)] public string? NombreTarifa { get; set; }
}
