namespace SistemaElectrico.Api.DTOs;

public class MedidorResumenDto
{
    public int Id { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
    public string TipoMedicion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInstalacion { get; set; }
    public DateTime? FechaRetiro { get; set; }
    public int SuministroId { get; set; }
    public string NumeroSuministro { get; set; } = string.Empty;
    public string NumeroContrato { get; set; } = string.Empty;
    public string Cuen { get; set; } = string.Empty;
    public string TitularActual { get; set; } = string.Empty;
}
