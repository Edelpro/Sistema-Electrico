namespace SistemaElectrico.Api.DTOs;

public sealed class SuministroResumenDto
{
    public int Id { get; set; }

    public string NumeroSuministro { get; set; } = string.Empty;

    public string NumeroContrato { get; set; } = string.Empty;

    public string Cuen { get; set; } = string.Empty;

    public string TitularActual { get; set; } = string.Empty;

    public string DireccionServicio { get; set; } = string.Empty;

    public string? Provincia { get; set; }

    public string? Canton { get; set; }

    public string? Parroquia { get; set; }

    public string? PlanGeocodigo { get; set; }

    public string? CodigoTarifa { get; set; }

    public string? NombreTarifa { get; set; }

    public string EstadoServicio { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }
}
