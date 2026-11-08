using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using SistemaElectrico.Api.Models;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaElectrico.Api.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = "Administrador")]
public sealed class ConfiguracionController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ConfiguracionCompletaDto>> Obtener()
    {
        var general = await context.ConfiguracionesSistema.AsNoTracking().SingleAsync();
        var politicas = await context.PoliticasSla.AsNoTracking()
            .OrderByDescending(x => x.Activa).ThenBy(x => x.TipoOrden).ThenBy(x => x.TipoZona)
            .ThenByDescending(x => x.Version).ToListAsync();
        return Ok(new ConfiguracionCompletaDto(Mapear(general), politicas.Select(Mapear).ToList()));
    }

    [HttpPut("general")]
    public async Task<ActionResult<ConfiguracionSistemaDto>> ActualizarGeneral(ActualizarConfiguracionDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var config = await context.ConfiguracionesSistema.SingleAsync();
        var anterior = JsonSerializer.Serialize(Mapear(config));
        config.NombreEmpresa = dto.NombreEmpresa.Trim();
        config.Ruc = dto.Ruc.Trim();
        config.CorreoSoporte = dto.CorreoSoporte.Trim().ToLowerInvariant();
        config.TelefonoSoporte = dto.TelefonoSoporte.Trim();
        config.Direccion = dto.Direccion.Trim();
        config.ZonaHoraria = dto.ZonaHoraria.Trim();
        config.TamanoMaximoEvidenciaMb = dto.TamanoMaximoEvidenciaMb;
        config.NotificacionesCorreoActivas = dto.NotificacionesCorreoActivas;
        config.NotificacionesWhatsAppActivas = dto.NotificacionesWhatsAppActivas;
        config.FechaActualizacionUtc = DateTime.UtcNow;
        config.ActualizadoPorUsuarioId = Actor();
        context.EventosAuditoria.Add(Auditoria("CONFIGURACION_ACTUALIZADA", nameof(ConfiguracionSistema),
            config.Id.ToString(), dto.Motivo.Trim(), anterior, JsonSerializer.Serialize(Mapear(config))));
        await context.SaveChangesAsync();
        return Ok(Mapear(config));
    }

    [HttpPost("politicas-sla")]
    public async Task<ActionResult<PoliticaSlaDto>> CrearPolitica(CrearPoliticaSlaDto dto)
    {
        var inicioValido = TimeSpan.TryParseExact(dto.HoraInicioLaboral, "hh\\:mm", CultureInfo.InvariantCulture, out var inicio);
        var finValido = TimeSpan.TryParseExact(dto.HoraFinLaboral, "hh\\:mm", CultureInfo.InvariantCulture, out var fin);
        if (!inicioValido || !finValido || inicio >= fin)
            ModelState.AddModelError(nameof(dto.HoraFinLaboral), "El horario laboral debe ser válido y la hora final posterior a la inicial.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var ahora = DateTime.UtcNow;
        var tipo = dto.TipoOrden.Trim().ToUpperInvariant();
        var zona = dto.TipoZona.Trim().ToUpperInvariant();
        var anteriores = await context.PoliticasSla.Where(x => x.TipoOrden == tipo && x.TipoZona == zona && x.Activa).ToListAsync();
        foreach (var anterior in anteriores) { anterior.Activa = false; anterior.VigenteHastaUtc = ahora; }
        var version = (await context.PoliticasSla.Where(x => x.TipoOrden == tipo && x.TipoZona == zona)
            .MaxAsync(x => (int?)x.Version) ?? 0) + 1;
        var politica = new PoliticaSla { Version = version, TipoOrden = tipo, TipoZona = zona,
            HorasObjetivo = dto.HorasObjetivo, HoraInicioLaboral = inicio, HoraFinLaboral = fin,
            IncluyeFinDeSemana = dto.IncluyeFinDeSemana, VigenteDesdeUtc = ahora, Activa = true,
            Motivo = dto.Motivo.Trim(), CreadoPorUsuarioId = Actor() };
        context.PoliticasSla.Add(politica);
        await context.SaveChangesAsync();
        context.EventosAuditoria.Add(Auditoria("POLITICA_SLA_VERSIONADA", nameof(PoliticaSla), politica.Id.ToString(),
            politica.Motivo, null, JsonSerializer.Serialize(Mapear(politica))));
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), Mapear(politica));
    }

    [HttpPatch("politicas-sla/{id:int}/estado")]
    public async Task<ActionResult<PoliticaSlaDto>> CambiarEstado(int id, CambiarEstadoPoliticaSlaDto dto)
    {
        var politica = await context.PoliticasSla.FindAsync(id);
        if (politica is null) return NotFound();
        if (dto.Activa) return BadRequest(new { mensaje = "Para activar una política crea una nueva versión; así se conserva el historial." });
        if (!politica.Activa) return Ok(Mapear(politica));
        politica.Activa = false;
        politica.VigenteHastaUtc = DateTime.UtcNow;
        context.EventosAuditoria.Add(Auditoria("POLITICA_SLA_DESACTIVADA", nameof(PoliticaSla), id.ToString(),
            dto.Motivo.Trim(), null, JsonSerializer.Serialize(Mapear(politica))));
        await context.SaveChangesAsync();
        return Ok(Mapear(politica));
    }

    private static ConfiguracionSistemaDto Mapear(ConfiguracionSistema x) => new(x.Id, x.NombreEmpresa, x.Ruc,
        x.CorreoSoporte, x.TelefonoSoporte, x.Direccion, x.ZonaHoraria, x.TamanoMaximoEvidenciaMb,
        x.NotificacionesCorreoActivas, x.NotificacionesWhatsAppActivas, x.FechaActualizacionUtc);
    private static PoliticaSlaDto Mapear(PoliticaSla x) => new(x.Id, x.Version, x.TipoOrden, x.TipoZona,
        x.HorasObjetivo, x.HoraInicioLaboral.ToString("hh\\:mm"), x.HoraFinLaboral.ToString("hh\\:mm"),
        x.IncluyeFinDeSemana, x.VigenteDesdeUtc, x.VigenteHastaUtc, x.Activa, x.Motivo);
    private EventoAuditoria Auditoria(string accion, string entidad, string id, string motivo, string? anterior, string? nuevo) =>
        new() { ActorUsuarioId = Actor(), Accion = accion, TipoEntidad = entidad, EntidadId = id,
            Motivo = motivo, ValorAnteriorJson = anterior, ValorNuevoJson = nuevo, OcurridoEnUtc = DateTime.UtcNow };
    private int? Actor() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
