using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using SistemaElectrico.Api.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace SistemaElectrico.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = "Tecnico")]
public sealed class PortalTecnicoController(ApplicationDbContext context, IWebHostEnvironment environment) : ControllerBase
{
    private const long TamanoMaximoSolicitud = 20 * 1024 * 1024;
    private static readonly Dictionary<string, string[]> Transiciones = new()
    {
        ["ASIGNADA"] = ["ACEPTADA"],
        ["ACEPTADA"] = ["EN_RUTA"],
        ["EN_RUTA"] = ["EN_SITIO"],
        ["EN_SITIO"] = ["EJECUTADA", "FALLIDA"],
        ["FALLIDA"] = ["REPROGRAMADA"]
    };
    private static readonly Dictionary<string, string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    [HttpGet]
    public async Task<ActionResult<PortalTecnicoResumenDto>> Resumen([FromQuery] string? buscar, [FromQuery] string? estado)
    {
        var actor = Actor();
        if (actor is null) return Unauthorized();

        var consulta = ConsultaAsignada(actor.Value).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(item => item.Codigo.Contains(termino)
                || item.Suministro.NumeroSuministro.Contains(termino)
                || item.Suministro.DireccionServicio.Contains(termino)
                || item.Causa.Contains(termino));
        }
        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("TODOS", StringComparison.OrdinalIgnoreCase))
        {
            var estadoNormalizado = estado.Trim().ToUpperInvariant();
            consulta = consulta.Where(item => item.Estado == estadoNormalizado);
        }

        var ordenes = await consulta.OrderByDescending(item => item.Prioridad == "CRITICA")
            .ThenBy(item => item.FechaLimiteSlaUtc).ThenByDescending(item => item.FechaActualizacionUtc).ToListAsync();
        var activas = ordenes.Where(item => item.Activa && item.Estado is not ("CERRADA" or "CANCELADA")).ToList();
        return Ok(new PortalTecnicoResumenDto(
            ordenes.Count,
            activas.Count(item => item.Estado is "ASIGNADA" or "REPROGRAMADA"),
            activas.Count(item => item.Estado is "ACEPTADA" or "EN_RUTA" or "EN_SITIO"),
            activas.Count(item => item.Prioridad == "CRITICA"),
            ordenes.Select(ResumenOrden).ToList()));
    }

    [HttpGet("ordenes/{id:int}")]
    public async Task<ActionResult<TecnicoOrdenDetalleDto>> ObtenerOrden(int id)
    {
        var actor = Actor();
        if (actor is null) return Unauthorized();
        var orden = await ConsultaAsignada(actor.Value).AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return orden is null ? NotFound() : Ok(DetalleOrden(orden));
    }

    [HttpPost("ordenes/{id:int}/transiciones")]
    public async Task<ActionResult<TecnicoOrdenDetalleDto>> Transicionar(int id, TransicionTecnicoDto dto)
    {
        var actor = Actor();
        if (actor is null) return Unauthorized();
        var orden = await ConsultaAsignada(actor.Value).FirstOrDefaultAsync(item => item.Id == id);
        if (orden is null) return NotFound();
        if (!orden.Activa || orden.Estado is "CERRADA" or "CANCELADA")
            return Conflict(new { codigo = "ORDEN_CERRADA", mensaje = "La orden ya no admite cambios." });

        var nuevo = dto.EstadoNuevo.Trim().ToUpperInvariant();
        if (!Transiciones.TryGetValue(orden.Estado, out var permitidos) || !permitidos.Contains(nuevo))
            return Conflict(new { codigo = "TRANSICION_INVALIDA", mensaje = $"No se puede pasar de {orden.Estado} a {nuevo}." });
        if (nuevo == "EJECUTADA" && orden.Evidencias.Count == 0)
            return Conflict(new { codigo = "EVIDENCIA_REQUERIDA", mensaje = "Registra al menos una evidencia fotográfica antes de finalizar el trabajo." });
        if (nuevo == "FALLIDA" && string.IsNullOrWhiteSpace(dto.Observacion))
            return Validacion(nameof(dto.Observacion), "Describe la novedad que impidió completar el trabajo.");

        var anterior = orden.Estado;
        orden.Estado = nuevo;
        orden.FechaActualizacionUtc = DateTime.UtcNow;
        orden.Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? orden.Observacion : dto.Observacion.Trim();
        orden.Historial.Add(new OrdenEstadoHistorial
        {
            EstadoAnterior = anterior,
            EstadoNuevo = nuevo,
            ActorUsuarioId = actor.Value,
            FechaUtc = DateTime.UtcNow,
            Motivo = dto.Motivo.Trim()
        });

        if (nuevo == "EJECUTADA")
        {
            orden.Suministro.EstadoServicio = orden.Tipo == "CORTE" ? "SUSPENDIDO" : "ACTIVO";
        }
        await Auditar(actor.Value, orden, "ORDEN_ESTADO_CAMBIADO", dto.Motivo,
            new { estadoAnterior = anterior, estadoNuevo = nuevo });
        await context.SaveChangesAsync();

        var guardada = await ConsultaAsignada(actor.Value).AsNoTracking().SingleAsync(item => item.Id == id);
        return Ok(DetalleOrden(guardada));
    }

    [HttpPost("ordenes/{id:int}/evidencias")]
    [RequestSizeLimit(TamanoMaximoSolicitud + 1024 * 64)]
    public async Task<ActionResult<EvidenciaTecnicaDto>> RegistrarEvidencia(int id, [FromForm] RegistrarEvidenciaDto dto)
    {
        var actor = Actor();
        if (actor is null) return Unauthorized();
        var orden = await ConsultaAsignada(actor.Value).FirstOrDefaultAsync(item => item.Id == id);
        if (orden is null) return NotFound();
        if (orden.Estado != "EN_SITIO")
            return Conflict(new { codigo = "ESTADO_INVALIDO", mensaje = "La evidencia solo puede registrarse cuando la orden está EN SITIO." });

        var foto = dto.Fotografia!;
        var limiteMb = await context.ConfiguracionesSistema.AsNoTracking()
            .Select(x => x.TamanoMaximoEvidenciaMb).SingleOrDefaultAsync();
        if (limiteMb <= 0) limiteMb = 5;
        var tamanoMaximo = limiteMb * 1024L * 1024L;
        if (foto.Length <= 0 || foto.Length > tamanoMaximo)
            return Validacion(nameof(dto.Fotografia), $"La fotografía debe pesar entre 1 byte y {limiteMb} MB.");
        if (!ExtensionesPermitidas.TryGetValue(foto.ContentType, out var extension))
            return Validacion(nameof(dto.Fotografia), "Solo se permiten fotografías JPG, PNG o WebP.");
        if (dto.FechaDispositivoUtc > DateTime.UtcNow.AddMinutes(10))
            return Validacion(nameof(dto.FechaDispositivoUtc), "La fecha del dispositivo no puede estar en el futuro.");

        var carpeta = Path.Combine(environment.ContentRootPath, "App_Data", "evidencias");
        Directory.CreateDirectory(carpeta);
        var archivoAlmacenado = $"{Guid.NewGuid():N}{extension}";
        var ruta = Path.Combine(carpeta, archivoAlmacenado);
        try
        {
            await using (var destino = System.IO.File.Create(ruta))
            {
                await foto.CopyToAsync(destino);
            }
            await using var lectura = System.IO.File.OpenRead(ruta);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(lectura));
            var evidencia = new EvidenciaOrden
            {
                OrdenTrabajoId = orden.Id,
                TecnicoUsuarioId = actor.Value,
                FechaDispositivoUtc = DateTime.SpecifyKind(dto.FechaDispositivoUtc, DateTimeKind.Utc),
                FechaServidorUtc = DateTime.UtcNow,
                Latitud = dto.Latitud,
                Longitud = dto.Longitud,
                PrecisionMetros = dto.PrecisionMetros,
                LecturaMedidor = dto.LecturaMedidor,
                Observacion = dto.Observacion.Trim(),
                NombreOriginal = Path.GetFileName(foto.FileName),
                ArchivoAlmacenado = archivoAlmacenado,
                TipoContenido = foto.ContentType,
                TamanoBytes = foto.Length,
                HashSha256 = hash
            };
            context.EvidenciasOrden.Add(evidencia);
            orden.FechaActualizacionUtc = DateTime.UtcNow;
            await Auditar(actor.Value, orden, "EVIDENCIA_REGISTRADA", dto.Observacion,
                new { evidencia.NombreOriginal, evidencia.TipoContenido, evidencia.TamanoBytes, evidencia.HashSha256 });
            await context.SaveChangesAsync();
            return CreatedAtAction(nameof(DescargarEvidencia), new { id = evidencia.Id }, MapearEvidencia(evidencia));
        }
        catch
        {
            if (System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);
            throw;
        }
    }

    [HttpGet("evidencias/{id:long}/archivo")]
    public async Task<IActionResult> DescargarEvidencia(long id)
    {
        var actor = Actor();
        if (actor is null) return Unauthorized();
        var evidencia = await context.EvidenciasOrden.AsNoTracking()
            .Include(item => item.OrdenTrabajo).FirstOrDefaultAsync(item => item.Id == id);
        if (evidencia is null || !await PerteneceAlTecnico(evidencia.OrdenTrabajo, actor.Value)) return NotFound();
        var ruta = Path.Combine(environment.ContentRootPath, "App_Data", "evidencias", evidencia.ArchivoAlmacenado);
        return System.IO.File.Exists(ruta)
            ? PhysicalFile(ruta, evidencia.TipoContenido, evidencia.NombreOriginal)
            : NotFound();
    }

    private IQueryable<OrdenTrabajo> ConsultaAsignada(int actor) => context.OrdenesTrabajo
        .Where(item => item.TecnicoUsuarioId == actor || (item.CuadrillaId != null
            && context.CuadrillaMiembros.Any(miembro => miembro.CuadrillaId == item.CuadrillaId
                && miembro.UsuarioId == actor && miembro.Activo)))
        .Include(item => item.Suministro).ThenInclude(item => item.ClienteTitular)
        .Include(item => item.Historial).ThenInclude(item => item.ActorUsuario)
        .Include(item => item.Evidencias);

    private async Task<bool> PerteneceAlTecnico(OrdenTrabajo orden, int actor) => orden.TecnicoUsuarioId == actor
        || (orden.CuadrillaId != null && await context.CuadrillaMiembros.AnyAsync(miembro =>
            miembro.CuadrillaId == orden.CuadrillaId && miembro.UsuarioId == actor && miembro.Activo));

    private static TecnicoOrdenResumenDto ResumenOrden(OrdenTrabajo orden) => new(
        orden.Id, orden.Codigo, orden.Suministro.NumeroSuministro, orden.Suministro.DireccionServicio,
        Zona(orden.Suministro), orden.Tipo, orden.Causa, orden.Estado, orden.Prioridad,
        orden.FechaLimiteSlaUtc, orden.FechaActualizacionUtc, orden.Evidencias.Count > 0);

    private static TecnicoOrdenDetalleDto DetalleOrden(OrdenTrabajo orden) => new(
        ResumenOrden(orden),
        $"{orden.Suministro.ClienteTitular.Nombres} {orden.Suministro.ClienteTitular.Apellidos}".Trim(),
        orden.Observacion,
        Transiciones.TryGetValue(orden.Estado, out var permitidas) ? permitidas : [],
        orden.Historial.OrderByDescending(item => item.FechaUtc).Select(item => new OrdenHistorialDto(
            item.EstadoAnterior, item.EstadoNuevo, item.FechaUtc, item.Motivo,
            item.ActorUsuario is null ? string.Empty : $"{item.ActorUsuario.Nombres} {item.ActorUsuario.Apellidos}".Trim())).ToList(),
        orden.Evidencias.OrderByDescending(item => item.FechaServidorUtc).Select(MapearEvidencia).ToList());

    private static EvidenciaTecnicaDto MapearEvidencia(EvidenciaOrden evidencia) => new(
        evidencia.Id, evidencia.FechaDispositivoUtc, evidencia.FechaServidorUtc,
        evidencia.PrecisionMetros, evidencia.LecturaMedidor, evidencia.Observacion, evidencia.NombreOriginal);

    private async Task Auditar(int actor, OrdenTrabajo orden, string accion, string motivo, object valorNuevo) =>
        await context.EventosAuditoria.AddAsync(new EventoAuditoria
        {
            ActorUsuarioId = actor,
            Accion = accion,
            TipoEntidad = nameof(OrdenTrabajo),
            EntidadId = orden.Id.ToString(),
            Motivo = motivo.Trim(),
            ValorNuevoJson = JsonSerializer.Serialize(valorNuevo),
            OcurridoEnUtc = DateTime.UtcNow
        });

    private int? Actor() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private ActionResult Validacion(string campo, string mensaje) => ValidationProblem(
        new ValidationProblemDetails(new Dictionary<string, string[]> { [campo] = [mensaje] }));
    private static string Zona(Suministro suministro) => string.Join(" · ",
        new[] { suministro.Provincia, suministro.Canton, suministro.Parroquia }.Where(valor => !string.IsNullOrWhiteSpace(valor)));
}
