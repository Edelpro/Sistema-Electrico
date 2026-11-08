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
public sealed class OrdenesTrabajoController(ApplicationDbContext context) : ControllerBase
{
    private static readonly HashSet<string> Tipos = ["CORTE", "RECONEXION"];
    private static readonly HashSet<string> Prioridades = ["NORMAL", "ALTA", "CRITICA"];
    private static readonly Dictionary<string, string[]> Transiciones = new()
    {
        ["CREADA"] = ["ASIGNADA", "CANCELADA"], ["ASIGNADA"] = ["ACEPTADA", "CANCELADA"],
        ["ACEPTADA"] = ["EN_RUTA", "CANCELADA"], ["EN_RUTA"] = ["EN_SITIO", "CANCELADA"],
        ["EN_SITIO"] = ["EJECUTADA", "FALLIDA"], ["FALLIDA"] = ["REPROGRAMADA"],
        ["REPROGRAMADA"] = ["ASIGNADA"], ["EJECUTADA"] = ["CERRADA"]
    };

    [HttpGet]
    public async Task<ActionResult<OrdenesPaginaDto>> Listar([FromQuery] string? buscar,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina); tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);
        var consulta = Completa().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim();
            consulta = consulta.Where(o => o.Codigo.Contains(t) || o.Suministro.NumeroSuministro.Contains(t) ||
                o.Causa.Contains(t) || o.Suministro.ClienteTitular.Nombres.Contains(t) ||
                o.Suministro.ClienteTitular.Apellidos.Contains(t) || (o.TecnicoUsuario != null && o.TecnicoUsuario.Correo.Contains(t)));
        }
        var total = await consulta.CountAsync();
        var items = await consulta.OrderByDescending(o => o.Activa).ThenByDescending(o => o.FechaCreacionUtc)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync();
        return Ok(new OrdenesPaginaDto(items.Select(Resumen).ToList(), total, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrdenTrabajoDetalleDto>> Obtener(int id)
    {
        var orden = await Completa().AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return orden is null ? NotFound() : Ok(Detalle(orden));
    }

    [HttpPost]
    public async Task<ActionResult<OrdenTrabajoDetalleDto>> Crear(CrearOrdenTrabajoDto dto)
    {
        var tipo = dto.Tipo.Trim().ToUpperInvariant(); var prioridad = dto.Prioridad.Trim().ToUpperInvariant();
        var causa = dto.Causa.Trim().ToUpperInvariant();
        if (!Tipos.Contains(tipo)) ModelState.AddModelError(nameof(dto.Tipo), "Tipo no válido.");
        if (!Prioridades.Contains(prioridad)) ModelState.AddModelError(nameof(dto.Prioridad), "Prioridad no válida.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var numero = dto.NumeroSuministro.Trim().ToUpperInvariant();
        var suministro = await context.Suministros.FirstOrDefaultAsync(s => s.NumeroSuministro == numero);
        if (suministro is null) return Problema(nameof(dto.NumeroSuministro), "No existe el suministro indicado.");
        if (await context.OrdenesTrabajo.AnyAsync(o => o.SuministroId == suministro.Id && o.Tipo == tipo && o.Causa == causa && o.Activa))
            return Conflict(new { codigo = "ORDEN_ACTIVA_DUPLICADA", mensaje = "Ya existe una orden activa del mismo tipo y causa." });
        var tecnico = await BuscarTecnico(dto.CorreoTecnico);
        if (!string.IsNullOrWhiteSpace(dto.CorreoTecnico) && tecnico is null)
            return Problema(nameof(dto.CorreoTecnico), "No existe un técnico activo con ese correo.");
        var cuadrilla = await BuscarCuadrilla(dto.CodigoCuadrilla);
        if (!string.IsNullOrWhiteSpace(dto.CodigoCuadrilla) && cuadrilla is null)
            return Problema(nameof(dto.CodigoCuadrilla), "No existe una cuadrilla disponible con ese código.");
        var ahora = DateTime.UtcNow;
        var orden = new OrdenTrabajo { Codigo = $"OT-{ahora:yyyyMMddHHmmssfff}-{suministro.Id}", Suministro = suministro,
            Tipo = tipo, Causa = causa, Prioridad = prioridad, Estado = tecnico is null && cuadrilla is null ? "CREADA" : "ASIGNADA",
            TecnicoUsuario = tecnico, Cuadrilla = cuadrilla, FechaLimiteSlaUtc = dto.FechaLimiteSlaUtc?.ToUniversalTime(),
            FechaCreacionUtc = ahora, FechaActualizacionUtc = ahora, Activa = true };
        orden.Historial.Add(NuevoHistorial(orden, null, orden.Estado, dto.Motivo.Trim()));
        context.OrdenesTrabajo.Add(orden); await context.SaveChangesAsync();
        await Auditar(orden, "ORDEN_CREADA", dto.Motivo); await context.SaveChangesAsync();
        var guardada = await Completa().AsNoTracking().SingleAsync(o => o.Id == orden.Id);
        return CreatedAtAction(nameof(Obtener), new { id = orden.Id }, Detalle(guardada));
    }

    [HttpPost("{id:int}/asignar")]
    public async Task<ActionResult<OrdenTrabajoDetalleDto>> Asignar(int id, AsignarOrdenDto dto)
    {
        var orden = await context.OrdenesTrabajo.Include(o => o.Historial).FirstOrDefaultAsync(o => o.Id == id);
        if (orden is null) return NotFound();
        if (!orden.Activa || orden.Estado is "CANCELADA" or "CERRADA") return Conflict(new { mensaje = "La orden ya está cerrada." });
        if (string.IsNullOrWhiteSpace(dto.CorreoTecnico) && string.IsNullOrWhiteSpace(dto.CodigoCuadrilla))
            return Problema(nameof(dto.CorreoTecnico), "Indica un técnico, una cuadrilla o ambos.");
        var tecnico = await BuscarTecnico(dto.CorreoTecnico);
        if (!string.IsNullOrWhiteSpace(dto.CorreoTecnico) && tecnico is null)
            return Problema(nameof(dto.CorreoTecnico), "No existe un técnico activo con ese correo.");
        var cuadrilla = await BuscarCuadrilla(dto.CodigoCuadrilla);
        if (!string.IsNullOrWhiteSpace(dto.CodigoCuadrilla) && cuadrilla is null)
            return Problema(nameof(dto.CodigoCuadrilla), "No existe una cuadrilla disponible con ese código.");
        var anterior = orden.Estado; orden.TecnicoUsuario = tecnico ?? orden.TecnicoUsuario;
        orden.Cuadrilla = cuadrilla ?? orden.Cuadrilla;
        if (orden.Estado is "CREADA" or "REPROGRAMADA") orden.Estado = "ASIGNADA";
        orden.FechaActualizacionUtc = DateTime.UtcNow;
        orden.Historial.Add(NuevoHistorial(orden, anterior, orden.Estado, dto.Motivo.Trim()));
        await Auditar(orden, "ORDEN_ASIGNADA", dto.Motivo); await context.SaveChangesAsync();
        return Ok(Detalle(await Completa().AsNoTracking().SingleAsync(o => o.Id == id)));
    }

    [HttpPost("{id:int}/transiciones")]
    public async Task<ActionResult<OrdenTrabajoDetalleDto>> Transicionar(int id, TransicionarOrdenDto dto)
    {
        var orden = await context.OrdenesTrabajo.Include(o => o.Historial).FirstOrDefaultAsync(o => o.Id == id);
        if (orden is null) return NotFound();
        var nuevo = dto.EstadoNuevo.Trim().ToUpperInvariant();
        if (!Transiciones.TryGetValue(orden.Estado, out var permitidos) || !permitidos.Contains(nuevo))
            return Conflict(new { codigo = "TRANSICION_INVALIDA", mensaje = $"No se puede pasar de {orden.Estado} a {nuevo}." });
        if (nuevo == "EJECUTADA" && !await context.EvidenciasOrden.AnyAsync(item => item.OrdenTrabajoId == orden.Id))
            return Conflict(new { codigo = "EVIDENCIA_REQUERIDA", mensaje = "La ejecución requiere al menos una evidencia fotográfica registrada por el técnico." });
        if (nuevo is "ASIGNADA" or "ACEPTADA" or "EN_RUTA" or "EN_SITIO"
            && orden.TecnicoUsuarioId is null && orden.CuadrillaId is null)
            return Conflict(new { codigo = "TECNICO_REQUERIDO", mensaje = "Asigna un técnico o una cuadrilla antes de avanzar la orden." });
        var anterior = orden.Estado; orden.Estado = nuevo; orden.Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? orden.Observacion : dto.Observacion.Trim();
        orden.FechaActualizacionUtc = DateTime.UtcNow; orden.Activa = nuevo is not ("CANCELADA" or "CERRADA");
        orden.Historial.Add(NuevoHistorial(orden, anterior, nuevo, dto.Motivo.Trim()));
        if (orden.Tipo == "CORTE" && nuevo == "EJECUTADA")
        {
            var suministro = await context.Suministros.FindAsync(orden.SuministroId);
            if (suministro is not null) suministro.EstadoServicio = "SUSPENDIDO";
        }
        if (orden.Tipo == "RECONEXION" && nuevo == "EJECUTADA")
        {
            var suministro = await context.Suministros.FindAsync(orden.SuministroId);
            if (suministro is not null) suministro.EstadoServicio = "ACTIVO";
        }
        await Auditar(orden, "ORDEN_ESTADO_CAMBIADO", dto.Motivo); await context.SaveChangesAsync();
        return Ok(Detalle(await Completa().AsNoTracking().SingleAsync(o => o.Id == id)));
    }

    private IQueryable<OrdenTrabajo> Completa() => context.OrdenesTrabajo.Include(o => o.Suministro).ThenInclude(s => s.ClienteTitular)
        .Include(o => o.TecnicoUsuario).Include(o => o.Cuadrilla).Include(o => o.Historial).ThenInclude(h => h.ActorUsuario);
    private async Task<Usuario?> BuscarTecnico(string? correo) => string.IsNullOrWhiteSpace(correo) ? null :
        await context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Correo == correo.Trim().ToLower() && u.Activo && u.Rol.Nombre == "Tecnico");
    private async Task<Cuadrilla?> BuscarCuadrilla(string? codigo) => string.IsNullOrWhiteSpace(codigo) ? null :
        await context.Cuadrillas.FirstOrDefaultAsync(c => c.Codigo == codigo.Trim().ToUpperInvariant() && c.Activa && c.Estado != "FUERA_SERVICIO");
    private OrdenEstadoHistorial NuevoHistorial(OrdenTrabajo orden, string? anterior, string nuevo, string motivo) =>
        new() { OrdenTrabajo = orden, EstadoAnterior = anterior, EstadoNuevo = nuevo, ActorUsuarioId = Actor(), FechaUtc = DateTime.UtcNow, Motivo = motivo };
    private async Task Auditar(OrdenTrabajo orden, string accion, string motivo) => context.EventosAuditoria.Add(new EventoAuditoria
        { ActorUsuarioId = Actor(), Accion = accion, TipoEntidad = nameof(OrdenTrabajo), EntidadId = orden.Id.ToString(), Motivo = motivo.Trim(),
          ValorNuevoJson = JsonSerializer.Serialize(new { orden.Codigo, orden.Estado, orden.Prioridad, orden.TecnicoUsuarioId }), OcurridoEnUtc = DateTime.UtcNow });
    private int? Actor() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private ActionResult Problema(string campo, string mensaje) => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { [campo] = [mensaje] }));
    private static string Nombre(Usuario? u) => u is null ? null! : $"{u.Nombres} {u.Apellidos}".Trim();
    private static string Titular(Suministro s) => $"{s.ClienteTitular.Nombres} {s.ClienteTitular.Apellidos}".Trim();
    private static string Zona(Suministro s) => string.Join(" · ", new[] { s.Provincia, s.Canton, s.Parroquia }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private static OrdenTrabajoResumenDto Resumen(OrdenTrabajo o) => new(o.Id, o.Codigo, o.Suministro.NumeroSuministro,
        Titular(o.Suministro), Zona(o.Suministro), o.Tipo, o.Causa, o.Estado, o.Prioridad, o.FechaLimiteSlaUtc, o.FechaCreacionUtc, Nombre(o.TecnicoUsuario), o.Cuadrilla?.Codigo);
    private static OrdenTrabajoDetalleDto Detalle(OrdenTrabajo o) => new(o.Id, o.Codigo, o.Suministro.NumeroSuministro,
        Titular(o.Suministro), o.Suministro.DireccionServicio, Zona(o.Suministro), o.Tipo, o.Causa, o.Estado,
        o.Prioridad, o.FechaLimiteSlaUtc, o.FechaCreacionUtc, o.FechaActualizacionUtc, o.Activa, Nombre(o.TecnicoUsuario),
        o.Cuadrilla?.Codigo, o.Observacion, o.PagoId, o.Historial.OrderByDescending(h => h.FechaUtc).Select(h => new OrdenHistorialDto(h.EstadoAnterior,
            h.EstadoNuevo, h.FechaUtc, h.Motivo, Nombre(h.ActorUsuario))).ToList());
}
