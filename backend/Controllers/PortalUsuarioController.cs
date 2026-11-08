using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using SistemaElectrico.Api.Models;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaElectrico.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = "Usuario")]
public sealed class PortalUsuarioController(ApplicationDbContext context, IWebHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> TiposReclamo =
        ["PAGO_NO_REFLEJADO", "RECONEXION_DEMORADA", "FACTURACION", "OTRO"];

    [HttpGet]
    public async Task<ActionResult<PortalUsuarioResumenDto>> Resumen()
    {
        var usuario = await UsuarioActual();
        if (usuario is null) return Forbid();
        if (usuario.ClienteId is null || usuario.Cliente is null) return CuentaSinVinculo();

        var suministros = await ConsultaSuministros(usuario.ClienteId.Value).ToListAsync();
        return Ok(new PortalUsuarioResumenDto(
            $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
            $"{usuario.Cliente.Nombres} {usuario.Cliente.Apellidos}".Trim(),
            suministros.Select(MapearResumen).ToList()));
    }

    [HttpGet("suministros/{id:int}")]
    public async Task<ActionResult<PortalSuministroDetalleDto>> Suministro(int id)
    {
        var usuario = await UsuarioActual();
        if (usuario?.ClienteId is null) return CuentaSinVinculo();

        var suministro = await ConsultaSuministros(usuario.ClienteId.Value)
            .Include(item => item.Facturas)
            .Include(item => item.Pagos)
            .Include(item => item.OrdenesTrabajo).ThenInclude(item => item.Historial)
            .Include(item => item.OrdenesTrabajo).ThenInclude(item => item.Evidencias)
            .Include(item => item.Reclamos).ThenInclude(item => item.Historial)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (suministro is null) return NotFound();

        var facturas = suministro.Facturas.OrderByDescending(item => item.FechaEmision)
            .Select(item => new PortalFacturaDto(item.Id, item.NumeroFactura, item.FechaEmision,
                item.FechaVencimiento, item.PeriodoDesde, item.PeriodoHasta, item.ConsumoFacturado,
                item.UnidadConsumo, item.Estado, item.TotalFactura, item.SaldoPendiente)).ToList();
        var pagos = suministro.Pagos.OrderByDescending(item => item.FechaConfirmacionUtc)
            .Select(item => new PortalPagoDto(item.Id, item.Referencia, item.Monto, item.Moneda,
                item.Estado, item.FechaConfirmacionUtc)).ToList();
        var ordenes = suministro.OrdenesTrabajo.OrderByDescending(item => item.FechaCreacionUtc)
            .Select(item =>
            {
                var evidencia = item.Evidencias.OrderByDescending(e => e.FechaServidorUtc).FirstOrDefault();
                return new PortalOrdenDto(item.Id, item.Codigo, item.Tipo, item.Estado, item.Prioridad,
                    item.FechaCreacionUtc, item.FechaLimiteSlaUtc, evidencia?.FechaServidorUtc, evidencia?.Id);
            }).ToList();
        var reclamos = suministro.Reclamos.OrderByDescending(item => item.FechaCreacionUtc)
            .Select(item => new PortalReclamoDto(item.Id, item.NumeroCaso, item.Tipo, item.Estado,
                item.Prioridad, item.Descripcion, item.Resolucion, item.FechaCreacionUtc, item.FechaCierreUtc)).ToList();

        var eventos = new List<PortalEventoDto>();
        eventos.AddRange(suministro.Facturas.Select(item => new PortalEventoDto("FACTURA",
            $"Factura {item.NumeroFactura}", $"Valor emitido: {item.TotalFactura:C2}", item.FechaEmision, item.Estado)));
        eventos.AddRange(suministro.Pagos.Select(item => new PortalEventoDto("PAGO",
            "Pago confirmado", $"Referencia {item.Referencia} por {item.Monto:C2}", item.FechaConfirmacionUtc, item.Estado)));
        eventos.AddRange(suministro.OrdenesTrabajo.SelectMany(orden => orden.Historial.Select(item =>
            new PortalEventoDto("ORDEN", orden.Tipo == "RECONEXION" ? "Reconexión" : "Orden de corte",
                item.Motivo, item.FechaUtc, item.EstadoNuevo))));
        eventos.AddRange(suministro.Reclamos.SelectMany(reclamo => reclamo.Historial.Select(item =>
            new PortalEventoDto("RECLAMO", $"Reclamo {reclamo.NumeroCaso}", item.Motivo,
                item.FechaUtc, item.EstadoNuevo))));

        return Ok(new PortalSuministroDetalleDto(MapearResumen(suministro), facturas, pagos, ordenes,
            reclamos, eventos.OrderByDescending(item => item.FechaUtc).Take(80).ToList()));
    }

    [HttpGet("facturas/{id:int}")]
    public async Task<ActionResult<FacturaDetalleDto>> Factura(int id)
    {
        var usuario = await UsuarioActual();
        if (usuario?.ClienteId is null) return CuentaSinVinculo();
        var factura = await context.Facturas.AsNoTracking()
            .Include(item => item.Suministro).ThenInclude(item => item.ClienteTitular)
            .Include(item => item.Medidor).Include(item => item.Lineas)
            .FirstOrDefaultAsync(item => item.Id == id && item.Suministro.ClienteTitularId == usuario.ClienteId);
        return factura is null ? NotFound() : Ok(MapearFactura(factura));
    }

    [HttpPost("reclamos")]
    public async Task<ActionResult<PortalReclamoDto>> CrearReclamo(CrearReclamoUsuarioDto dto)
    {
        var usuario = await UsuarioActual();
        if (usuario?.ClienteId is null) return CuentaSinVinculo();
        var tipo = dto.Tipo.Trim().ToUpperInvariant();
        if (!TiposReclamo.Contains(tipo))
            ModelState.AddModelError(nameof(dto.Tipo), "Selecciona un tipo de reclamo válido.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var numero = dto.NumeroSuministro.Trim().ToUpperInvariant();
        var suministro = await context.Suministros.FirstOrDefaultAsync(item =>
            item.ClienteTitularId == usuario.ClienteId && item.NumeroSuministro == numero);
        if (suministro is null) return NotFound();
        if (dto.PagoId.HasValue && !await context.Pagos.AnyAsync(item => item.Id == dto.PagoId && item.SuministroId == suministro.Id))
            return Problema(nameof(dto.PagoId), "El pago no pertenece al suministro seleccionado.");
        if (dto.OrdenTrabajoId.HasValue && !await context.OrdenesTrabajo.AnyAsync(item => item.Id == dto.OrdenTrabajoId && item.SuministroId == suministro.Id))
            return Problema(nameof(dto.OrdenTrabajoId), "La orden no pertenece al suministro seleccionado.");

        var ahora = DateTime.UtcNow;
        var reclamo = new Reclamo
        {
            NumeroCaso = $"CAS-{ahora:yyyyMMddHHmmssfff}-{suministro.Id}",
            Suministro = suministro,
            PagoId = dto.PagoId,
            OrdenTrabajoId = dto.OrdenTrabajoId,
            CreadoPorUsuarioId = usuario.Id,
            Tipo = tipo,
            Estado = "ABIERTO",
            Prioridad = "NORMAL",
            Descripcion = dto.Descripcion.Trim(),
            FechaCreacionUtc = ahora,
            FechaActualizacionUtc = ahora
        };
        reclamo.Historial.Add(new ReclamoHistorial
        {
            Reclamo = reclamo,
            EstadoNuevo = "ABIERTO",
            ActorUsuarioId = usuario.Id,
            Motivo = "Reclamo creado desde el portal del cliente.",
            FechaUtc = ahora
        });
        context.Reclamos.Add(reclamo);
        await context.SaveChangesAsync();
        context.EventosAuditoria.Add(new EventoAuditoria
        {
            ActorUsuarioId = usuario.Id,
            Accion = "RECLAMO_CLIENTE_CREADO",
            TipoEntidad = nameof(Reclamo),
            EntidadId = reclamo.Id.ToString(),
            ValorNuevoJson = JsonSerializer.Serialize(new { reclamo.NumeroCaso, reclamo.Tipo, reclamo.Estado }),
            Motivo = "Creación autenticada desde el portal del cliente.",
            OcurridoEnUtc = ahora
        });
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Suministro), new { id = suministro.Id }, new PortalReclamoDto(
            reclamo.Id, reclamo.NumeroCaso, reclamo.Tipo, reclamo.Estado, reclamo.Prioridad,
            reclamo.Descripcion, reclamo.Resolucion, reclamo.FechaCreacionUtc, reclamo.FechaCierreUtc));
    }

    [HttpGet("evidencias/{id:long}/archivo")]
    public async Task<IActionResult> Evidencia(long id)
    {
        var usuario = await UsuarioActual();
        if (usuario?.ClienteId is null) return CuentaSinVinculo();
        var evidencia = await context.EvidenciasOrden.AsNoTracking()
            .Include(item => item.OrdenTrabajo).ThenInclude(item => item.Suministro)
            .FirstOrDefaultAsync(item => item.Id == id &&
                item.OrdenTrabajo.Suministro.ClienteTitularId == usuario.ClienteId);
        if (evidencia is null) return NotFound();
        var ruta = Path.Combine(environment.ContentRootPath, "App_Data", "evidencias", evidencia.ArchivoAlmacenado);
        return System.IO.File.Exists(ruta)
            ? PhysicalFile(ruta, evidencia.TipoContenido, evidencia.NombreOriginal)
            : NotFound();
    }

    private async Task<Usuario?> UsuarioActual()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        return await context.Usuarios.AsNoTracking().Include(item => item.Cliente)
            .FirstOrDefaultAsync(item => item.Id == id && item.Activo);
    }

    private IQueryable<Suministro> ConsultaSuministros(int clienteId) => context.Suministros.AsNoTracking().AsSplitQuery()
        .Include(item => item.Facturas).Include(item => item.OrdenesTrabajo)
        .Include(item => item.Reclamos).Where(item => item.ClienteTitularId == clienteId);

    private static PortalSuministroResumenDto MapearResumen(Suministro item)
    {
        var activa = item.OrdenesTrabajo.Where(orden => orden.Activa)
            .OrderByDescending(orden => orden.FechaCreacionUtc).FirstOrDefault();
        return new PortalSuministroResumenDto(item.Id, item.NumeroSuministro, item.NumeroContrato,
            item.DireccionServicio, item.EstadoServicio, item.Facturas.Sum(f => f.SaldoPendiente),
            item.Facturas.Count(f => f.SaldoPendiente > 0), activa?.Estado, activa?.FechaLimiteSlaUtc,
            item.Reclamos.Count(r => r.Estado is "ABIERTO" or "EN_REVISION"));
    }

    private static FacturaDetalleDto MapearFactura(Factura f) => new()
    {
        Id = f.Id, NumeroFactura = f.NumeroFactura, NumeroSuministro = f.Suministro.NumeroSuministro,
        NumeroContrato = f.Suministro.NumeroContrato, Cuen = f.Suministro.Cuen,
        TitularActual = $"{f.Suministro.ClienteTitular.Nombres} {f.Suministro.ClienteTitular.Apellidos}".Trim(),
        NumeroMedidor = f.Medidor?.NumeroSerie, SistemaOrigen = f.SistemaOrigen,
        IdentificadorExterno = f.IdentificadorExterno, NombreEmisor = f.NombreEmisor, RucEmisor = f.RucEmisor,
        FechaEmision = f.FechaEmision, FechaVencimiento = f.FechaVencimiento, PeriodoDesde = f.PeriodoDesde,
        PeriodoHasta = f.PeriodoHasta, Moneda = f.Moneda, MetodoLectura = f.MetodoLectura,
        LecturaAnterior = f.LecturaAnterior, LecturaActual = f.LecturaActual, ConsumoFacturado = f.ConsumoFacturado,
        UnidadConsumo = f.UnidadConsumo, Estado = f.Estado, SubtotalServicioElectrico = f.SubtotalServicioElectrico,
        SubtotalAlumbradoPublico = f.SubtotalAlumbradoPublico, OtrosCargos = f.OtrosCargos,
        SaldoAnterior = f.SaldoAnterior, RecaudacionTerceros = f.RecaudacionTerceros,
        Subsidios = f.Subsidios, TotalFactura = f.TotalFactura, SaldoPendiente = f.SaldoPendiente,
        TieneDiferencia = f.TieneDiferencia, SincronizadaEnUtc = f.SincronizadaEnUtc,
        Lineas = f.Lineas.OrderBy(item => item.NumeroLinea).Select(item => new FacturaLineaDto
        {
            NumeroLinea = item.NumeroLinea, CodigoOriginal = item.CodigoOriginal,
            DescripcionOriginal = item.DescripcionOriginal, Categoria = item.Categoria,
            Monto = item.Monto, EsIngresoElectrico = item.EsIngresoElectrico
        }).ToList()
    };

    private ObjectResult CuentaSinVinculo() => Conflict(new
    {
        codigo = "CLIENTE_NO_VINCULADO",
        mensaje = "La cuenta de acceso todavía no está vinculada a un cliente. Solicita al administrador que complete la vinculación."
    });

    private ActionResult Problema(string campo, string mensaje) => ValidationProblem(
        new ValidationProblemDetails(new Dictionary<string, string[]> { [campo] = [mensaje] }));
}
