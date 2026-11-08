namespace SistemaElectrico.Api.DTOs;

public sealed record AuditoriaResumenDto(long Id, string Actor, string Accion, string TipoEntidad,
    string EntidadId, string? Motivo, DateTime OcurridoEnUtc);
public sealed record AuditoriaPaginaDto(IReadOnlyCollection<AuditoriaResumenDto> Items, int Total,
    int Pagina, int TamanoPagina);
public sealed record ReporteOperativoDto(int ClientesActivos, int SuministrosActivos, int FacturasPendientes,
    decimal SaldoPendiente, int OrdenesActivas, int OrdenesSlaVencido, int ReclamosAbiertos,
    decimal PagosConfirmadosMes, DateTime GeneradoEnUtc);
