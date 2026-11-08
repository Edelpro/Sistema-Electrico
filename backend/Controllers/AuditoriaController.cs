using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using System.Text;

namespace SistemaElectrico.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = "Administrador")]
public sealed class AuditoriaController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AuditoriaPaginaDto>> Listar([FromQuery] string? buscar,
        [FromQuery] string? accion, [FromQuery] DateTime? desdeUtc, [FromQuery] DateTime? hastaUtc,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina); tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);
        var q = context.EventosAuditoria.AsNoTracking().Include(x => x.ActorUsuario).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar)) {
            var t = buscar.Trim();
            q = q.Where(x => x.EntidadId.Contains(t) || x.TipoEntidad.Contains(t) ||
                (x.Motivo != null && x.Motivo.Contains(t)) ||
                (x.ActorUsuario != null && (x.ActorUsuario.Nombres.Contains(t) || x.ActorUsuario.Apellidos.Contains(t))));
        }
        if (!string.IsNullOrWhiteSpace(accion)) q = q.Where(x => x.Accion == accion.Trim().ToUpperInvariant());
        if (desdeUtc.HasValue) q = q.Where(x => x.OcurridoEnUtc >= desdeUtc.Value);
        if (hastaUtc.HasValue) q = q.Where(x => x.OcurridoEnUtc <= hastaUtc.Value);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(x => x.OcurridoEnUtc).Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(x => new AuditoriaResumenDto(x.Id,
                x.ActorUsuario == null ? "Sistema" : (x.ActorUsuario.Nombres + " " + x.ActorUsuario.Apellidos).Trim(),
                x.Accion, x.TipoEntidad, x.EntidadId, x.Motivo, x.OcurridoEnUtc)).ToListAsync();
        return Ok(new AuditoriaPaginaDto(items, total, pagina, tamanoPagina));
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<ReporteOperativoDto>> Resumen()
    {
        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return Ok(new ReporteOperativoDto(
            await context.Clientes.CountAsync(x => x.Activo),
            await context.Suministros.CountAsync(x => x.EstadoServicio == "ACTIVO"),
            await context.Facturas.CountAsync(x => x.SaldoPendiente > 0),
            await context.Facturas.SumAsync(x => (decimal?)x.SaldoPendiente) ?? 0,
            await context.OrdenesTrabajo.CountAsync(x => x.Activa),
            await context.OrdenesTrabajo.CountAsync(x => x.Activa && x.FechaLimiteSlaUtc < ahora),
            await context.Reclamos.CountAsync(x => x.Estado == "ABIERTO" || x.Estado == "EN_REVISION"),
            await context.Pagos.Where(x => x.FechaConfirmacionUtc >= inicioMes).SumAsync(x => (decimal?)x.Monto) ?? 0,
            ahora));
    }

    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar([FromQuery] DateTime? desdeUtc, [FromQuery] DateTime? hastaUtc)
    {
        var q = context.EventosAuditoria.AsNoTracking().Include(x => x.ActorUsuario).AsQueryable();
        if (desdeUtc.HasValue) q = q.Where(x => x.OcurridoEnUtc >= desdeUtc.Value);
        if (hastaUtc.HasValue) q = q.Where(x => x.OcurridoEnUtc <= hastaUtc.Value);
        var items = await q.OrderByDescending(x => x.OcurridoEnUtc).Take(10000).ToListAsync();
        var csv = new StringBuilder("Fecha UTC,Actor,Acción,Entidad,ID,Motivo\r\n");
        foreach (var x in items) csv.AppendLine(string.Join(',',
            Csv(x.OcurridoEnUtc.ToString("O")), Csv(x.ActorUsuario is null ? "Sistema" : $"{x.ActorUsuario.Nombres} {x.ActorUsuario.Apellidos}".Trim()),
            Csv(x.Accion), Csv(x.TipoEntidad), Csv(x.EntidadId), Csv(x.Motivo ?? string.Empty)));
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8",
            $"auditoria-sgse-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    private static string Csv(string valor)
    {
        if (valor.Length > 0 && "=+-@".Contains(valor[0])) valor = "'" + valor;
        return $"\"{valor.Replace("\"", "\"\"")}\"";
    }
}
