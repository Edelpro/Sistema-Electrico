using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using SistemaElectrico.Api.Models;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaElectrico.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = "Administrador")]
public sealed class ReclamosController(ApplicationDbContext context) : ControllerBase
{
    private static readonly HashSet<string> Tipos = ["PAGO_NO_REFLEJADO", "RECONEXION_DEMORADA", "FACTURACION", "OTRO"];
    private static readonly HashSet<string> Prioridades = ["NORMAL", "ALTA", "CRITICA"];
    private static readonly Dictionary<string, string[]> Transiciones = new()
    {
        ["ABIERTO"] = ["EN_REVISION", "CANCELADO"],
        ["EN_REVISION"] = ["RESUELTO", "CANCELADO"],
        ["RESUELTO"] = ["CERRADO", "EN_REVISION"]
    };

    [HttpGet]
    public async Task<ActionResult<ReclamosPaginaDto>> Listar([FromQuery] string? buscar,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina); tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);
        var consulta = Completa().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim();
            consulta = consulta.Where(r => r.NumeroCaso.Contains(t) || r.Suministro.NumeroSuministro.Contains(t) ||
                r.Descripcion.Contains(t) || r.Suministro.ClienteTitular.Nombres.Contains(t) ||
                r.Suministro.ClienteTitular.Apellidos.Contains(t));
        }
        var total = await consulta.CountAsync();
        var items = await consulta.OrderBy(r => r.Estado == "CERRADO" || r.Estado == "CANCELADO")
            .ThenByDescending(r => r.Prioridad == "CRITICA").ThenByDescending(r => r.FechaCreacionUtc)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync();
        return Ok(new ReclamosPaginaDto(items.Select(Resumen).ToList(), total, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReclamoDetalleDto>> Obtener(int id)
    {
        var entidad = await Completa().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return entidad is null ? NotFound() : Ok(Detalle(entidad));
    }

    [HttpPost]
    public async Task<ActionResult<ReclamoDetalleDto>> Crear(CrearReclamoDto dto)
    {
        var tipo = dto.Tipo.Trim().ToUpperInvariant(); var prioridad = dto.Prioridad.Trim().ToUpperInvariant();
        if (!Tipos.Contains(tipo)) ModelState.AddModelError(nameof(dto.Tipo), "Tipo de reclamo no válido.");
        if (!Prioridades.Contains(prioridad)) ModelState.AddModelError(nameof(dto.Prioridad), "Prioridad no válida.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var numero = dto.NumeroSuministro.Trim().ToUpperInvariant();
        var suministro = await context.Suministros.FirstOrDefaultAsync(s => s.NumeroSuministro == numero);
        if (suministro is null) return Problema(nameof(dto.NumeroSuministro), "No existe el suministro indicado.");
        if (dto.PagoId.HasValue && !await context.Pagos.AnyAsync(p => p.Id == dto.PagoId && p.SuministroId == suministro.Id))
            ModelState.AddModelError(nameof(dto.PagoId), "El pago no pertenece al suministro.");
        if (dto.OrdenTrabajoId.HasValue && !await context.OrdenesTrabajo.AnyAsync(o => o.Id == dto.OrdenTrabajoId && o.SuministroId == suministro.Id))
            ModelState.AddModelError(nameof(dto.OrdenTrabajoId), "La orden no pertenece al suministro.");
        var asignado = await BuscarAsignado(dto.CorreoAsignado);
        if (!string.IsNullOrWhiteSpace(dto.CorreoAsignado) && asignado is null)
            ModelState.AddModelError(nameof(dto.CorreoAsignado), "No existe un administrador activo con ese correo.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var ahora = DateTime.UtcNow;
        var entidad = new Reclamo { NumeroCaso = $"REC-{ahora:yyyyMMddHHmmssfff}-{suministro.Id}",
            Suministro = suministro, PagoId = dto.PagoId, OrdenTrabajoId = dto.OrdenTrabajoId,
            CreadoPorUsuarioId = Actor(), AsignadoAUsuario = asignado, Tipo = tipo, Prioridad = prioridad,
            Descripcion = dto.Descripcion.Trim(), Estado = asignado is null ? "ABIERTO" : "EN_REVISION",
            FechaCreacionUtc = ahora, FechaActualizacionUtc = ahora };
        entidad.Historial.Add(Historial(entidad, null, entidad.Estado, "Caso de soporte registrado."));
        context.Reclamos.Add(entidad); await context.SaveChangesAsync();
        Auditar(entidad, "RECLAMO_CREADO", "Caso de soporte registrado."); await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = entidad.Id }, Detalle(await Completa().AsNoTracking().SingleAsync(r => r.Id == entidad.Id)));
    }

    [HttpPost("{id:int}/gestionar")]
    public async Task<ActionResult<ReclamoDetalleDto>> Gestionar(int id, GestionarReclamoDto dto)
    {
        var entidad = await context.Reclamos.Include(r => r.Historial).FirstOrDefaultAsync(r => r.Id == id);
        if (entidad is null) return NotFound();
        var nuevo = dto.EstadoNuevo.Trim().ToUpperInvariant();
        if (!Transiciones.TryGetValue(entidad.Estado, out var permitidos) || !permitidos.Contains(nuevo))
            return Conflict(new { codigo = "TRANSICION_INVALIDA", mensaje = $"No se puede pasar de {entidad.Estado} a {nuevo}." });
        if (nuevo is "RESUELTO" or "CERRADO" && string.IsNullOrWhiteSpace(dto.Resolucion) && string.IsNullOrWhiteSpace(entidad.Resolucion))
            return Problema(nameof(dto.Resolucion), "Escribe la resolución antes de resolver o cerrar el caso.");
        var asignado = await BuscarAsignado(dto.CorreoAsignado);
        if (!string.IsNullOrWhiteSpace(dto.CorreoAsignado) && asignado is null)
            return Problema(nameof(dto.CorreoAsignado), "No existe un administrador activo con ese correo.");
        var anterior = entidad.Estado; entidad.Estado = nuevo;
        if (!string.IsNullOrWhiteSpace(dto.Resolucion)) entidad.Resolucion = dto.Resolucion.Trim();
        if (asignado is not null) entidad.AsignadoAUsuario = asignado;
        entidad.FechaActualizacionUtc = DateTime.UtcNow;
        if (nuevo is "CERRADO" or "CANCELADO") entidad.FechaCierreUtc = DateTime.UtcNow;
        else entidad.FechaCierreUtc = null;
        entidad.Historial.Add(Historial(entidad, anterior, nuevo, dto.Motivo.Trim()));
        Auditar(entidad, "RECLAMO_GESTIONADO", dto.Motivo, anterior); await context.SaveChangesAsync();
        return Ok(Detalle(await Completa().AsNoTracking().SingleAsync(r => r.Id == id)));
    }

    private IQueryable<Reclamo> Completa() => context.Reclamos.Include(r => r.Suministro).ThenInclude(s => s.ClienteTitular)
        .Include(r => r.AsignadoAUsuario).Include(r => r.Historial).ThenInclude(h => h.ActorUsuario);
    private async Task<Usuario?> BuscarAsignado(string? correo) => string.IsNullOrWhiteSpace(correo) ? null :
        await context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Correo == correo.Trim().ToLowerInvariant() && u.Activo && u.Rol.Nombre == "Administrador");
    private ReclamoHistorial Historial(Reclamo r, string? anterior, string nuevo, string motivo) => new() { Reclamo = r, EstadoAnterior = anterior, EstadoNuevo = nuevo, Motivo = motivo, ActorUsuarioId = Actor(), FechaUtc = DateTime.UtcNow };
    private void Auditar(Reclamo r, string accion, string motivo, string? anterior = null) => context.EventosAuditoria.Add(new EventoAuditoria { ActorUsuarioId = Actor(), Accion = accion, TipoEntidad = nameof(Reclamo), EntidadId = r.Id.ToString(), Motivo = motivo.Trim(), ValorAnteriorJson = anterior is null ? null : JsonSerializer.Serialize(new { Estado = anterior }), ValorNuevoJson = JsonSerializer.Serialize(new { r.NumeroCaso, r.Estado, r.Prioridad, r.AsignadoAUsuarioId }), OcurridoEnUtc = DateTime.UtcNow });
    private int? Actor() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private ActionResult Problema(string campo, string mensaje) => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { [campo] = [mensaje] }));
    private static string Nombre(Usuario? u) => u is null ? string.Empty : $"{u.Nombres} {u.Apellidos}".Trim();
    private static string Titular(Reclamo r) => $"{r.Suministro.ClienteTitular.Nombres} {r.Suministro.ClienteTitular.Apellidos}".Trim();
    private static ReclamoResumenDto Resumen(Reclamo r) => new(r.Id, r.NumeroCaso, r.Suministro.NumeroSuministro, Titular(r), r.Tipo, r.Estado, r.Prioridad, r.FechaCreacionUtc, string.IsNullOrEmpty(Nombre(r.AsignadoAUsuario)) ? null : Nombre(r.AsignadoAUsuario));
    private static ReclamoDetalleDto Detalle(Reclamo r) => new(r.Id, r.NumeroCaso, r.Suministro.NumeroSuministro, Titular(r), r.Suministro.DireccionServicio, r.Tipo, r.Estado, r.Prioridad, r.Descripcion, r.Resolucion, r.PagoId, r.OrdenTrabajoId, string.IsNullOrEmpty(Nombre(r.AsignadoAUsuario)) ? null : Nombre(r.AsignadoAUsuario), r.FechaCreacionUtc, r.FechaActualizacionUtc, r.FechaCierreUtc, r.Historial.OrderByDescending(h => h.FechaUtc).Select(h => new ReclamoHistorialDto(h.EstadoAnterior, h.EstadoNuevo, h.Motivo, h.FechaUtc, string.IsNullOrEmpty(Nombre(h.ActorUsuario)) ? null : Nombre(h.ActorUsuario))).ToList());
}
