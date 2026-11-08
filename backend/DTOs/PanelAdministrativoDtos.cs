namespace SistemaElectrico.Api.DTOs;

public sealed record IndicadoresPanelDto(int ClientesActivos, int OrdenesPendientes,
    int CuadrillasDisponibles, int ReclamosAbiertos, int SlaPorVencer, int SlaVencidos);
public sealed record ConsumoMensualPanelDto(int Anio, int Mes, decimal ConsumoKwh, decimal TotalFacturado);
public sealed record ReconexionPrioritariaDto(int Id, string Codigo, string NumeroSuministro,
    string Titular, string Zona, string Estado, DateTime? FechaLimiteSlaUtc);
public sealed record ActividadPanelDto(long Id, string Actor, string Accion, string Entidad,
    string EntidadId, string? Motivo, DateTime FechaUtc);
public sealed record PanelAdministrativoDto(IndicadoresPanelDto Indicadores,
    IReadOnlyCollection<ConsumoMensualPanelDto> ConsumoMensual,
    IReadOnlyCollection<ReconexionPrioritariaDto> ReconexionesPrioritarias,
    IReadOnlyCollection<ActividadPanelDto> ActividadReciente);
public sealed record ResultadoBusquedaGlobalDto(string Tipo, string Titulo, string Subtitulo,
    string Ruta);
