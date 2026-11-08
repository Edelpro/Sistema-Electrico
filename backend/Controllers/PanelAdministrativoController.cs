using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;

namespace SistemaElectrico.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = "Administrador")]
public sealed class PanelAdministrativoController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PanelAdministrativoDto>> Obtener()
    {
        var ahora = DateTime.UtcNow;
        var limiteAlerta = ahora.AddHours(2);
        var pendientes = new[] { "CREADA", "ASIGNADA", "EN_RUTA", "EN_SITIO", "EN_EJECUCION" };
        var indicadores = new IndicadoresPanelDto(
            await context.Clientes.CountAsync(x => x.Activo),
            await context.OrdenesTrabajo.CountAsync(x => x.Activa && pendientes.Contains(x.Estado)),
            await context.Cuadrillas.CountAsync(x => x.Activa && x.Estado == "DISPONIBLE"),
            await context.Reclamos.CountAsync(x => x.Estado == "ABIERTO" || x.Estado == "EN_REVISION"),
            await context.OrdenesTrabajo.CountAsync(x => x.Activa && x.FechaLimiteSlaUtc > ahora && x.FechaLimiteSlaUtc <= limiteAlerta),
            await context.OrdenesTrabajo.CountAsync(x => x.Activa && x.FechaLimiteSlaUtc < ahora));

        var desde = new DateTime(ahora.Year, ahora.Month, 1).AddMonths(-5);
        var facturasPeriodo = await context.Facturas.AsNoTracking().Where(x => x.PeriodoHasta >= desde)
            .Select(x => new { x.PeriodoHasta, x.ConsumoFacturado, x.TotalFactura }).ToListAsync();
        var consumos = facturasPeriodo.GroupBy(x => new { x.PeriodoHasta.Year, x.PeriodoHasta.Month })
            .Select(g => new ConsumoMensualPanelDto(g.Key.Year, g.Key.Month,
                g.Sum(x => x.ConsumoFacturado), g.Sum(x => x.TotalFactura)))
            .OrderBy(x => x.Anio).ThenBy(x => x.Mes).ToList();

        var reconexiones = await context.OrdenesTrabajo.AsNoTracking().Include(x => x.Suministro)
            .ThenInclude(x => x.ClienteTitular)
            .Where(x => x.Activa && x.Tipo == "RECONEXION" && pendientes.Contains(x.Estado))
            .OrderBy(x => x.FechaLimiteSlaUtc ?? DateTime.MaxValue).ThenByDescending(x => x.Prioridad)
            .Take(5).Select(x => new ReconexionPrioritariaDto(x.Id, x.Codigo, x.Suministro.NumeroSuministro,
                (x.Suministro.ClienteTitular.Nombres + " " + x.Suministro.ClienteTitular.Apellidos).Trim(),
                (x.Suministro.Canton ?? x.Suministro.Parroquia) ?? "Sin zona", x.Estado, x.FechaLimiteSlaUtc)).ToListAsync();

        var actividad = await context.EventosAuditoria.AsNoTracking().Include(x => x.ActorUsuario)
            .OrderByDescending(x => x.OcurridoEnUtc).Take(8)
            .Select(x => new ActividadPanelDto(x.Id,
                x.ActorUsuario == null ? "Sistema" : (x.ActorUsuario.Nombres + " " + x.ActorUsuario.Apellidos).Trim(),
                x.Accion, x.TipoEntidad, x.EntidadId, x.Motivo, x.OcurridoEnUtc)).ToListAsync();
        return Ok(new PanelAdministrativoDto(indicadores, consumos, reconexiones, actividad));
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<IReadOnlyCollection<ResultadoBusquedaGlobalDto>>> Buscar([FromQuery] string termino)
    {
        termino = (termino ?? string.Empty).Trim();
        if (termino.Length < 2) return Ok(Array.Empty<ResultadoBusquedaGlobalDto>());
        var resultados = new List<ResultadoBusquedaGlobalDto>();
        resultados.AddRange(await context.Clientes.AsNoTracking()
            .Where(x => x.Identificacion.Contains(termino) || x.Nombres.Contains(termino) || x.Apellidos.Contains(termino))
            .Take(4).Select(x => new ResultadoBusquedaGlobalDto("Cliente", (x.Nombres + " " + x.Apellidos).Trim(),
                x.Identificacion, "/admin/clientes?q=" + x.Identificacion)).ToListAsync());
        resultados.AddRange(await context.Suministros.AsNoTracking().Include(x => x.ClienteTitular)
            .Where(x => x.NumeroSuministro.Contains(termino) || x.NumeroContrato.Contains(termino) || x.Cuen.Contains(termino))
            .Take(4).Select(x => new ResultadoBusquedaGlobalDto("Suministro", x.NumeroSuministro,
                (x.ClienteTitular.Nombres + " " + x.ClienteTitular.Apellidos).Trim(), "/admin/suministros?q=" + x.NumeroSuministro)).ToListAsync());
        resultados.AddRange(await context.OrdenesTrabajo.AsNoTracking()
            .Where(x => x.Codigo.Contains(termino)).Take(4)
            .Select(x => new ResultadoBusquedaGlobalDto("Orden", x.Codigo, x.Estado,
                "/admin/ordenes?q=" + x.Codigo)).ToListAsync());
        return Ok(resultados.Take(10).ToList());
    }
}
