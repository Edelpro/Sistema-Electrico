namespace SistemaElectrico.Api.DTOs;

public sealed class MedidoresPaginaDto
{
    public IReadOnlyList<MedidorResumenDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
}
