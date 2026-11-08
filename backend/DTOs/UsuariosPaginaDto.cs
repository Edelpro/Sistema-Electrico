namespace SistemaElectrico.Api.DTOs;

public sealed class UsuariosPaginaDto
{
    public IReadOnlyList<UsuarioResumenDto> Items { get; set; } = [];

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
