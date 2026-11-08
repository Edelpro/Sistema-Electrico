namespace SistemaElectrico.Api.DTOs;

public sealed class SuministrosPaginaDto
{
    public IReadOnlyList<SuministroResumenDto> Items { get; set; } = [];

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
