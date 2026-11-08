using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using SistemaElectrico.Api.Models;
using SistemaElectrico.Api.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaElectrico.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = "Administrador")]
public sealed class PagosController(ApplicationDbContext context, SlaService slaService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagosPaginaDto>> Listar([FromQuery] string? buscar,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina); tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);
        var consulta = context.Pagos.AsNoTracking().Include(p => p.Suministro).ThenInclude(s => s.ClienteTitular)
            .Include(p => p.Asignaciones).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim();
            consulta = consulta.Where(p => p.Referencia.Contains(t) || p.EventoProveedorId.Contains(t) ||
                p.Proveedor.Contains(t) || p.Suministro.NumeroSuministro.Contains(t) ||
                p.Suministro.ClienteTitular.Nombres.Contains(t) || p.Suministro.ClienteTitular.Apellidos.Contains(t));
        }
        var total = await consulta.CountAsync();
        var items = await consulta.OrderByDescending(p => p.FechaConfirmacionUtc).ThenByDescending(p => p.Id)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync();
        return Ok(new PagosPaginaDto(items.Select(Resumen).ToList(), total, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PagoDetalleDto>> Obtener(int id)
    {
        var pago = await Completa().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return pago is null ? NotFound() : Ok(Detalle(pago));
    }

    [HttpPost("conciliar")]
    public async Task<ActionResult<ResultadoConciliacionPagoDto>> Conciliar(ConciliarPagoDto dto)
    {
        var proveedor = dto.Proveedor.Trim().ToUpperInvariant();
        var evento = dto.EventoProveedorId.Trim().ToUpperInvariant();
        var existente = await Completa().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Proveedor == proveedor && p.EventoProveedorId == evento);
        if (existente is not null)
            return Ok(new ResultadoConciliacionPagoDto(Detalle(existente), true,
                new("SIN_CAMBIOS", "El evento ya había sido procesado; no se duplicó el pago.", null, null)));

        if (dto.FechaProveedorUtc == default) ModelState.AddModelError(nameof(dto.FechaProveedorUtc), "Indica la fecha del proveedor.");
        if (dto.Asignaciones.Count == 0) ModelState.AddModelError(nameof(dto.Asignaciones), "Agrega al menos una factura.");
        if (dto.Asignaciones.GroupBy(a => a.NumeroFactura.Trim().ToUpperInvariant()).Any(g => g.Count() > 1))
            ModelState.AddModelError(nameof(dto.Asignaciones), "No repitas una factura en el mismo pago.");
        var totalAplicado = dto.Asignaciones.Sum(a => a.MontoAplicado);
        if (totalAplicado > dto.Monto) ModelState.AddModelError(nameof(dto.Monto), "El total aplicado no puede superar el pago.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var numeroSuministro = dto.NumeroSuministro.Trim().ToUpperInvariant();
        var suministro = await context.Suministros.Include(s => s.ClienteTitular)
            .FirstOrDefaultAsync(s => s.NumeroSuministro == numeroSuministro);
        if (suministro is null) return ValidationProblem(new ValidationProblemDetails(
            new Dictionary<string, string[]> { [nameof(dto.NumeroSuministro)] = ["No existe el suministro indicado."] }));

        var numeros = dto.Asignaciones.Select(a => a.NumeroFactura.Trim().ToUpperInvariant()).ToList();
        var facturas = await context.Facturas.Where(f => f.SuministroId == suministro.Id && numeros.Contains(f.NumeroFactura)).ToListAsync();
        foreach (var asignacion in dto.Asignaciones)
        {
            var numero = asignacion.NumeroFactura.Trim().ToUpperInvariant();
            var factura = facturas.FirstOrDefault(f => f.NumeroFactura == numero);
            if (factura is null) ModelState.AddModelError(nameof(dto.Asignaciones), $"La factura {numero} no pertenece al suministro.");
            else if (asignacion.MontoAplicado > factura.SaldoPendiente)
                ModelState.AddModelError(nameof(dto.Asignaciones), $"El monto supera el saldo de {numero}.");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        await using var transaccion = await context.Database.BeginTransactionAsync();
        try
        {
            var ahora = DateTime.UtcNow;
            var pago = new Pago { Suministro = suministro, Proveedor = proveedor, EventoProveedorId = evento,
                Referencia = dto.Referencia.Trim().ToUpperInvariant(), Monto = dto.Monto,
                Estado = totalAplicado == dto.Monto ? "APLICADO" : "APLICADO_PARCIAL",
                FechaProveedorUtc = dto.FechaProveedorUtc.ToUniversalTime(), FechaRecepcionUtc = ahora,
                FechaConfirmacionUtc = ahora, MotivoConciliacion = dto.Motivo.Trim() };
            context.Pagos.Add(pago);
            foreach (var item in dto.Asignaciones)
            {
                var factura = facturas.Single(f => f.NumeroFactura == item.NumeroFactura.Trim().ToUpperInvariant());
                factura.SaldoPendiente -= item.MontoAplicado;
                factura.Estado = factura.SaldoPendiente == 0 ? "PAGADA" : "PARCIALMENTE_PAGADA";
                pago.Asignaciones.Add(new PagoAsignacion { Factura = factura, MontoAplicado = item.MontoAplicado, AplicadoEnUtc = ahora });
            }

            ResultadoOperativoPagoDto resultado;
            var corte = await context.OrdenesTrabajo.Where(o => o.SuministroId == suministro.Id && o.Tipo == "CORTE" && o.Activa && o.Estado != "EJECUTADA")
                .OrderByDescending(o => o.Id).FirstOrDefaultAsync();
            if (corte is not null)
            {
                var anterior = corte.Estado; corte.Estado = "CANCELADA"; corte.Activa = false; corte.FechaActualizacionUtc = ahora;
                corte.Historial.Add(Historial(corte, anterior, "CANCELADA", "Pago confirmado; corte cancelado automáticamente."));
                resultado = new("CORTE_CANCELADO", "Se canceló el corte pendiente asociado al suministro.", corte.Id, corte.Codigo);
            }
            else if (await context.OrdenesTrabajo.AnyAsync(o => o.SuministroId == suministro.Id && o.Tipo == "CORTE" &&
                (o.Estado == "EJECUTADA" || o.Estado == "CERRADA")))
            {
                var reconexion = await context.OrdenesTrabajo.FirstOrDefaultAsync(o => o.SuministroId == suministro.Id && o.Tipo == "RECONEXION" && o.Activa);
                if (reconexion is null)
                {
                    reconexion = new OrdenTrabajo { Codigo = $"OT-{ahora:yyyyMMddHHmmss}-{suministro.Id}", Suministro = suministro,
                        Pago = pago, Tipo = "RECONEXION", Causa = "PAGO_CONFIRMADO", Prioridad = "ALTA", Estado = "CREADA",
                        FechaLimiteSlaUtc = await slaService.CalcularLimiteAsync(suministro, "RECONEXION", ahora),
                        FechaCreacionUtc = ahora, FechaActualizacionUtc = ahora, Activa = true };
                    reconexion.Historial.Add(Historial(reconexion, null, "CREADA", "Reconexión creada automáticamente por pago confirmado."));
                    context.OrdenesTrabajo.Add(reconexion);
                    resultado = new("RECONEXION_CREADA", reconexion.FechaLimiteSlaUtc is null
                        ? "Se creó la reconexión; falta una política SLA activa para su zona."
                        : "Se creó la reconexión con fecha límite calculada según la política SLA vigente.", null, reconexion.Codigo);
                }
                else resultado = new("RECONEXION_REUTILIZADA", "Ya existía una reconexión activa; no se duplicó.", reconexion.Id, reconexion.Codigo);
            }
            else resultado = new("SIN_ORDEN", "Pago confirmado; no había un corte activo que cancelar o reconectar.", null, null);

            await context.SaveChangesAsync();
            context.EventosAuditoria.Add(new EventoAuditoria { ActorUsuarioId = Actor(), Accion = "PAGO_CONCILIADO",
                TipoEntidad = nameof(Pago), EntidadId = pago.Id.ToString(), Motivo = dto.Motivo.Trim(),
                ValorNuevoJson = JsonSerializer.Serialize(new { pago.Proveedor, pago.EventoProveedorId, pago.Monto, pago.Estado }), OcurridoEnUtc = ahora });
            await context.SaveChangesAsync(); await transaccion.CommitAsync();
            var guardado = await Completa().AsNoTracking().SingleAsync(p => p.Id == pago.Id);
            return CreatedAtAction(nameof(Obtener), new { id = pago.Id }, new ResultadoConciliacionPagoDto(Detalle(guardado), false, resultado));
        }
        catch { await transaccion.RollbackAsync(); throw; }
    }

    private IQueryable<Pago> Completa() => context.Pagos.Include(p => p.Suministro).ThenInclude(s => s.ClienteTitular)
        .Include(p => p.Asignaciones).ThenInclude(a => a.Factura);
    private static PagoResumenDto Resumen(Pago p) => new(p.Id, p.Suministro.NumeroSuministro,
        $"{p.Suministro.ClienteTitular.Nombres} {p.Suministro.ClienteTitular.Apellidos}".Trim(), p.Proveedor, p.Referencia,
        p.Monto, p.Estado, p.FechaConfirmacionUtc, p.Asignaciones.Count);
    private static PagoDetalleDto Detalle(Pago p) => new(p.Id, p.Suministro.NumeroSuministro,
        $"{p.Suministro.ClienteTitular.Nombres} {p.Suministro.ClienteTitular.Apellidos}".Trim(), p.Proveedor,
        p.EventoProveedorId, p.Referencia, p.Monto, p.Moneda, p.Estado, p.FechaProveedorUtc, p.FechaRecepcionUtc,
        p.FechaConfirmacionUtc, p.MotivoConciliacion, p.Asignaciones.Select(a => new PagoAsignacionDto(a.FacturaId, a.Factura.NumeroFactura, a.MontoAplicado)).ToList());
    private OrdenEstadoHistorial Historial(OrdenTrabajo orden, string? anterior, string nuevo, string motivo) =>
        new() { OrdenTrabajo = orden, EstadoAnterior = anterior, EstadoNuevo = nuevo, ActorUsuarioId = Actor(), FechaUtc = DateTime.UtcNow, Motivo = motivo };
    private int? Actor() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
