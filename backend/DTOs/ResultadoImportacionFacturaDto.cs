namespace SistemaElectrico.Api.DTOs;

public sealed class ResultadoImportacionFacturaDto
{
    public FacturaDetalleDto Factura { get; set; } = null!;
    public bool YaExistia { get; set; }
}
