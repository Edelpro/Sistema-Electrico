namespace SistemaElectrico.Api.DTOs;

public sealed class FacturasPaginaDto
{
    public List<FacturaResumenDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
