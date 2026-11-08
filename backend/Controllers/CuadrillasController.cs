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
public sealed class CuadrillasController(ApplicationDbContext context) : ControllerBase
{
    private static readonly HashSet<string> Estados = ["DISPONIBLE", "OCUPADA", "FUERA_SERVICIO", "INACTIVA"];

    [HttpGet]
    public async Task<ActionResult<CuadrillasPaginaDto>> Listar([FromQuery] string? buscar,
        [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina); tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);
        var consulta = Completa().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim();
            consulta = consulta.Where(c => c.Codigo.Contains(t) || c.Nombre.Contains(t) || c.Zona.Contains(t) ||
                c.Miembros.Any(m => m.Activo && (m.Usuario.Nombres.Contains(t) || m.Usuario.Apellidos.Contains(t) || m.Usuario.Correo.Contains(t))));
        }
        var total = await consulta.CountAsync();
        var items = await consulta.OrderByDescending(c => c.Activa).ThenBy(c => c.Codigo)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync();
        return Ok(new CuadrillasPaginaDto(items.Select(Resumen).ToList(), total, pagina, tamanoPagina));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CuadrillaDetalleDto>> Obtener(int id)
    {
        var entidad = await Completa().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return entidad is null ? NotFound() : Ok(Detalle(entidad));
    }

    [HttpPost]
    public async Task<ActionResult<CuadrillaDetalleDto>> Crear(GuardarCuadrillaDto dto)
    {
        var datos = Normalizar(dto); Validar(datos);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (await context.Cuadrillas.AnyAsync(c => c.Codigo == datos.Codigo))
            return Problema(nameof(dto.Codigo), "Ya existe una cuadrilla con este código.");
        var ahora = DateTime.UtcNow;
        var entidad = new Cuadrilla { Codigo = datos.Codigo, Nombre = datos.Nombre, Zona = datos.Zona,
            CapacidadMaxima = datos.CapacidadMaxima, Estado = datos.Estado, Activa = datos.Estado != "INACTIVA",
            Observaciones = datos.Observaciones, FechaCreacionUtc = ahora, FechaActualizacionUtc = ahora };
        context.Cuadrillas.Add(entidad); await context.SaveChangesAsync();
        Auditar(entidad, "CUADRILLA_CREADA", datos.Motivo, null); await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = entidad.Id }, Detalle(await Completa().AsNoTracking().SingleAsync(c => c.Id == entidad.Id)));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CuadrillaDetalleDto>> Actualizar(int id, GuardarCuadrillaDto dto)
    {
        var entidad = await context.Cuadrillas.FirstOrDefaultAsync(c => c.Id == id);
        if (entidad is null) return NotFound();
        var datos = Normalizar(dto); Validar(datos);
        if (await context.Cuadrillas.AnyAsync(c => c.Id != id && c.Codigo == datos.Codigo))
            ModelState.AddModelError(nameof(dto.Codigo), "Ya existe una cuadrilla con este código.");
        var miembros = await context.CuadrillaMiembros.CountAsync(m => m.CuadrillaId == id && m.Activo);
        if (datos.CapacidadMaxima < miembros)
            ModelState.AddModelError(nameof(dto.CapacidadMaxima), $"La capacidad no puede ser menor que los {miembros} integrantes activos.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var anterior = JsonSerializer.Serialize(new { entidad.Codigo, entidad.Nombre, entidad.Zona, entidad.CapacidadMaxima, entidad.Estado });
        entidad.Codigo = datos.Codigo; entidad.Nombre = datos.Nombre; entidad.Zona = datos.Zona;
        entidad.CapacidadMaxima = datos.CapacidadMaxima; entidad.Estado = datos.Estado;
        entidad.Activa = datos.Estado != "INACTIVA"; entidad.Observaciones = datos.Observaciones;
        entidad.FechaActualizacionUtc = DateTime.UtcNow;
        Auditar(entidad, "CUADRILLA_ACTUALIZADA", datos.Motivo, anterior); await context.SaveChangesAsync();
        return Ok(Detalle(await Completa().AsNoTracking().SingleAsync(c => c.Id == id)));
    }

    [HttpPost("{id:int}/estado")]
    public async Task<ActionResult<CuadrillaDetalleDto>> CambiarEstado(int id, CambiarEstadoCuadrillaDto dto)
    {
        var entidad = await context.Cuadrillas.FirstOrDefaultAsync(c => c.Id == id);
        if (entidad is null) return NotFound();
        var estado = dto.Estado.Trim().ToUpperInvariant();
        if (!Estados.Contains(estado)) return Problema(nameof(dto.Estado), "Estado de cuadrilla no válido.");
        if (estado == "INACTIVA" && await context.OrdenesTrabajo.AnyAsync(o => o.CuadrillaId == id && o.Activa))
            return Conflict(new { codigo = "CUADRILLA_CON_ORDENES", mensaje = "No puedes desactivar una cuadrilla con órdenes activas." });
        var anterior = entidad.Estado; entidad.Estado = estado; entidad.Activa = estado != "INACTIVA";
        entidad.FechaActualizacionUtc = DateTime.UtcNow;
        Auditar(entidad, "CUADRILLA_ESTADO_CAMBIADO", dto.Motivo, JsonSerializer.Serialize(new { Estado = anterior }));
        await context.SaveChangesAsync(); return Ok(Detalle(await Completa().AsNoTracking().SingleAsync(c => c.Id == id)));
    }

    [HttpPost("{id:int}/miembros")]
    public async Task<ActionResult<CuadrillaDetalleDto>> AgregarMiembro(int id, AgregarMiembroCuadrillaDto dto)
    {
        var entidad = await context.Cuadrillas.Include(c => c.Miembros).FirstOrDefaultAsync(c => c.Id == id);
        if (entidad is null) return NotFound();
        if (!entidad.Activa) return Conflict(new { mensaje = "Activa la cuadrilla antes de agregar integrantes." });
        if (entidad.Miembros.Count(m => m.Activo) >= entidad.CapacidadMaxima)
            return Conflict(new { codigo = "CAPACIDAD_COMPLETA", mensaje = "La cuadrilla alcanzó su capacidad máxima." });
        var correo = dto.CorreoTecnico.Trim().ToLowerInvariant();
        var usuario = await context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Correo == correo && u.Activo && u.Rol.Nombre == "Tecnico");
        if (usuario is null) return Problema(nameof(dto.CorreoTecnico), "No existe un técnico activo con ese correo.");
        if (await context.CuadrillaMiembros.AnyAsync(m => m.UsuarioId == usuario.Id && m.Activo))
            return Conflict(new { codigo = "TECNICO_YA_ASIGNADO", mensaje = "El técnico ya pertenece a una cuadrilla activa." });
        var previo = await context.CuadrillaMiembros.FirstOrDefaultAsync(m => m.CuadrillaId == id && m.UsuarioId == usuario.Id && !m.Activo);
        if (previo is null) context.CuadrillaMiembros.Add(new CuadrillaMiembro { Cuadrilla = entidad, Usuario = usuario, Activo = true });
        else { previo.Activo = true; previo.FechaAsignacionUtc = DateTime.UtcNow; previo.FechaFinUtc = null; }
        entidad.FechaActualizacionUtc = DateTime.UtcNow; Auditar(entidad, "CUADRILLA_MIEMBRO_AGREGADO", dto.Motivo, null, new { usuario.Id, usuario.Correo });
        await context.SaveChangesAsync(); return Ok(Detalle(await Completa().AsNoTracking().SingleAsync(c => c.Id == id)));
    }

    [HttpPost("{id:int}/miembros/{miembroId:int}/retirar")]
    public async Task<ActionResult<CuadrillaDetalleDto>> RetirarMiembro(int id, int miembroId, RetirarMiembroCuadrillaDto dto)
    {
        var miembro = await context.CuadrillaMiembros.Include(m => m.Cuadrilla).Include(m => m.Usuario)
            .FirstOrDefaultAsync(m => m.Id == miembroId && m.CuadrillaId == id && m.Activo);
        if (miembro is null) return NotFound();
        if (await context.OrdenesTrabajo.AnyAsync(o => o.TecnicoUsuarioId == miembro.UsuarioId && o.CuadrillaId == id && o.Activa))
            return Conflict(new { codigo = "TECNICO_CON_ORDEN", mensaje = "Reasigna las órdenes activas del técnico antes de retirarlo." });
        miembro.Activo = false; miembro.FechaFinUtc = DateTime.UtcNow; miembro.Cuadrilla.FechaActualizacionUtc = DateTime.UtcNow;
        Auditar(miembro.Cuadrilla, "CUADRILLA_MIEMBRO_RETIRADO", dto.Motivo, new { miembro.UsuarioId, miembro.Usuario.Correo });
        await context.SaveChangesAsync(); return Ok(Detalle(await Completa().AsNoTracking().SingleAsync(c => c.Id == id)));
    }

    private IQueryable<Cuadrilla> Completa() => context.Cuadrillas
        .Include(c => c.Miembros).ThenInclude(m => m.Usuario).Include(c => c.Ordenes);
    private void Validar(GuardarCuadrillaDto dto) { if (!Estados.Contains(dto.Estado)) ModelState.AddModelError(nameof(dto.Estado), "Estado no válido."); }
    private static GuardarCuadrillaDto Normalizar(GuardarCuadrillaDto d) => new() { Codigo = d.Codigo.Trim().ToUpperInvariant(), Nombre = d.Nombre.Trim(), Zona = d.Zona.Trim(), CapacidadMaxima = d.CapacidadMaxima, Estado = d.Estado.Trim().ToUpperInvariant(), Observaciones = string.IsNullOrWhiteSpace(d.Observaciones) ? null : d.Observaciones.Trim(), Motivo = d.Motivo.Trim() };
    private static CuadrillaResumenDto Resumen(Cuadrilla c) => new(c.Id, c.Codigo, c.Nombre, c.Zona, c.CapacidadMaxima, c.Miembros.Count(m => m.Activo), c.Estado, c.Activa, c.Ordenes.Count(o => o.Activa));
    private static CuadrillaDetalleDto Detalle(Cuadrilla c) => new(c.Id, c.Codigo, c.Nombre, c.Zona, c.CapacidadMaxima, c.Estado, c.Activa, c.Observaciones, c.FechaCreacionUtc, c.FechaActualizacionUtc, c.Miembros.Where(m => m.Activo).OrderBy(m => m.Usuario.Apellidos).Select(m => new CuadrillaMiembroDto(m.Id, m.UsuarioId, $"{m.Usuario.Nombres} {m.Usuario.Apellidos}".Trim(), m.Usuario.Correo, m.FechaAsignacionUtc)).ToList(), c.Ordenes.Count(o => o.Activa));
    private void Auditar(Cuadrilla c, string accion, string motivo, object? anterior = null, object? nuevo = null) => context.EventosAuditoria.Add(new EventoAuditoria { ActorUsuarioId = Actor(), Accion = accion, TipoEntidad = nameof(Cuadrilla), EntidadId = c.Id.ToString(), Motivo = motivo.Trim(), ValorAnteriorJson = anterior is null ? null : anterior is string s ? s : JsonSerializer.Serialize(anterior), ValorNuevoJson = JsonSerializer.Serialize(nuevo ?? new { c.Codigo, c.Nombre, c.Zona, c.CapacidadMaxima, c.Estado }), OcurridoEnUtc = DateTime.UtcNow });
    private int? Actor() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private ActionResult Problema(string campo, string mensaje) => ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { [campo] = [mensaje] }));
}
