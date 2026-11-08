using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using SistemaElectrico.Api.Models;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SistemaElectrico.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public sealed class MedidoresController : ControllerBase
{
    private static readonly string[] TiposValidos = ["CONVENCIONAL", "INTELIGENTE", "OTRO"];
    private static readonly string[] EstadosValidos = ["ACTIVO", "RETIRADO", "DEFECTUOSO", "REEMPLAZADO"];
    private readonly ApplicationDbContext _context;

    public MedidoresController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<MedidoresPaginaDto>> Listar(
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);
        var consulta = _context.Medidores.AsNoTracking()
            .Include(item => item.Suministro).ThenInclude(item => item.ClienteTitular).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(item => item.NumeroSerie.Contains(termino) ||
                item.Suministro.NumeroSuministro.Contains(termino) ||
                item.Suministro.NumeroContrato.Contains(termino) || item.Suministro.Cuen.Contains(termino) ||
                item.Suministro.ClienteTitular.Nombres.Contains(termino) ||
                item.Suministro.ClienteTitular.Apellidos.Contains(termino));
        }

        var total = await consulta.CountAsync();
        var entidades = await consulta.OrderBy(item => item.NumeroSerie)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync();
        return Ok(new MedidoresPaginaDto
        {
            Items = entidades.Select(CrearResumen).ToList(), Total = total,
            Pagina = pagina, TamanoPagina = tamanoPagina
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MedidorDetalleDto>> ObtenerPorId(int id)
    {
        var item = await ConsultaCompleta().AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return item is null ? NotFound() : Ok(CrearDetalle(item));
    }

    [HttpPost]
    public async Task<ActionResult<MedidorDetalleDto>> Crear(CrearMedidorDto solicitud)
    {
        var datos = Normalizar(solicitud);
        Validar(datos);
        var suministro = await BuscarSuministroActivo(datos.NumeroSuministro);
        if (suministro is null)
            ModelState.AddModelError(nameof(CrearMedidorDto.NumeroSuministro),
                "No existe un suministro activo con este número.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (await _context.Medidores.AnyAsync(item => item.NumeroSerie == datos.NumeroSerie))
            return Conflict(new { codigo = "SERIE_DUPLICADA", mensaje = "Ya existe un medidor con este número de serie." });
        if (await TieneMedidorActivo(suministro!.Id)) return ConflictoMedidorActivo();

        var medidor = new Medidor
        {
            NumeroSerie = datos.NumeroSerie, TipoMedicion = datos.TipoMedicion,
            Estado = "ACTIVO", FechaInstalacion = datos.FechaInstalacion,
            FechaRegistro = DateTime.UtcNow, SuministroId = suministro.Id, Suministro = suministro
        };
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Medidores.Add(medidor);
            await _context.SaveChangesAsync();
            RegistrarAuditoria("MEDIDOR_CREADO", medidor, null, CrearSnapshot(medidor),
                "Registro desde el portal administrativo.");
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("crear");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = medidor.Id }, CrearDetalle(medidor));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MedidorDetalleDto>> Actualizar(int id, ActualizarMedidorDto solicitud)
    {
        var medidor = await ConsultaCompleta().FirstOrDefaultAsync(item => item.Id == id);
        if (medidor is null) return NotFound();
        var datos = Normalizar(solicitud);
        Validar(datos);
        var motivo = NormalizarTexto(solicitud.Motivo);
        if (motivo.Length < 5)
            ModelState.AddModelError(nameof(ActualizarMedidorDto.Motivo),
                "Describe el motivo administrativo con al menos 5 caracteres.");
        var suministro = await BuscarSuministroActivo(datos.NumeroSuministro);
        if (suministro is null)
            ModelState.AddModelError(nameof(CrearMedidorDto.NumeroSuministro),
                "No existe un suministro activo con este número.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (await _context.Medidores.AnyAsync(item => item.Id != id && item.NumeroSerie == datos.NumeroSerie))
            return Conflict(new { codigo = "SERIE_DUPLICADA", mensaje = "Ya existe un medidor con este número de serie." });
        if (medidor.Estado == "ACTIVO" && medidor.SuministroId != suministro!.Id &&
            await TieneMedidorActivo(suministro.Id, id)) return ConflictoMedidorActivo();

        var anterior = CrearSnapshot(medidor);
        medidor.NumeroSerie = datos.NumeroSerie;
        medidor.TipoMedicion = datos.TipoMedicion;
        medidor.FechaInstalacion = datos.FechaInstalacion;
        medidor.SuministroId = suministro!.Id;
        medidor.Suministro = suministro;
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            RegistrarAuditoria("MEDIDOR_ACTUALIZADO", medidor, anterior, CrearSnapshot(medidor), motivo);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("actualizar");
        }
        return Ok(CrearDetalle(medidor));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<MedidorDetalleDto>> CambiarEstado(int id, CambiarEstadoMedidorDto solicitud)
    {
        var medidor = await ConsultaCompleta().FirstOrDefaultAsync(item => item.Id == id);
        if (medidor is null) return NotFound();
        var estado = NormalizarTexto(solicitud.Estado).ToUpperInvariant();
        var motivo = NormalizarTexto(solicitud.Motivo);
        if (!EstadosValidos.Contains(estado))
            ModelState.AddModelError(nameof(CambiarEstadoMedidorDto.Estado), "Selecciona un estado válido.");
        if (motivo.Length < 5)
            ModelState.AddModelError(nameof(CambiarEstadoMedidorDto.Motivo),
                "Describe el motivo con al menos 5 caracteres.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (medidor.Estado == estado) return Ok(CrearDetalle(medidor));
        if (estado == "ACTIVO" && medidor.Suministro.EstadoServicio != "ACTIVO")
            return Conflict(new { codigo = "SUMINISTRO_INACTIVO", mensaje = "El suministro debe estar activo para activar el medidor." });
        if (estado == "ACTIVO" && await TieneMedidorActivo(medidor.SuministroId, id))
            return ConflictoMedidorActivo();

        var anterior = CrearSnapshot(medidor);
        medidor.Estado = estado;
        medidor.FechaRetiro = estado is "RETIRADO" or "REEMPLAZADO" ? DateTime.UtcNow : null;
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            RegistrarAuditoria("MEDIDOR_ESTADO_CAMBIADO", medidor, anterior, CrearSnapshot(medidor), motivo);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("cambiar el estado de");
        }
        return Ok(CrearDetalle(medidor));
    }

    private IQueryable<Medidor> ConsultaCompleta() => _context.Medidores
        .Include(item => item.Suministro).ThenInclude(item => item.ClienteTitular);

    private async Task<Suministro?> BuscarSuministroActivo(string numero) =>
        await _context.Suministros.Include(item => item.ClienteTitular)
            .FirstOrDefaultAsync(item => item.NumeroSuministro == numero && item.EstadoServicio == "ACTIVO");

    private async Task<bool> TieneMedidorActivo(int suministroId, int? excluirId = null) =>
        await _context.Medidores.AnyAsync(item => item.SuministroId == suministroId &&
            item.Estado == "ACTIVO" && (!excluirId.HasValue || item.Id != excluirId.Value));

    private void Validar(MedidorNormalizado datos)
    {
        if (!Regex.IsMatch(datos.NumeroSerie, "^[A-Z0-9-]{3,50}$"))
            ModelState.AddModelError(nameof(CrearMedidorDto.NumeroSerie),
                "La serie debe contener de 3 a 50 letras, números o guiones.");
        if (!Regex.IsMatch(datos.NumeroSuministro, "^[A-Z0-9-]{3,30}$"))
            ModelState.AddModelError(nameof(CrearMedidorDto.NumeroSuministro),
                "Escribe un número de suministro válido.");
        if (!TiposValidos.Contains(datos.TipoMedicion))
            ModelState.AddModelError(nameof(CrearMedidorDto.TipoMedicion), "Selecciona un tipo de medición válido.");
        if (datos.FechaInstalacion == default || datos.FechaInstalacion.Date > DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(CrearMedidorDto.FechaInstalacion),
                "La fecha de instalación es obligatoria y no puede estar en el futuro.");
    }

    private void RegistrarAuditoria(string accion, Medidor item, string? anterior, string nuevo, string motivo) =>
        _context.EventosAuditoria.Add(new EventoAuditoria
        {
            ActorUsuarioId = ObtenerActorUsuarioId(), Accion = accion, TipoEntidad = nameof(Medidor),
            EntidadId = item.Id.ToString(), ValorAnteriorJson = anterior, ValorNuevoJson = nuevo,
            OcurridoEnUtc = DateTime.UtcNow, Motivo = motivo
        });

    private int? ObtenerActorUsuarioId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static MedidorResumenDto CrearResumen(Medidor item) => new()
    {
        Id = item.Id, NumeroSerie = item.NumeroSerie, TipoMedicion = item.TipoMedicion,
        Estado = item.Estado, FechaInstalacion = item.FechaInstalacion, FechaRetiro = item.FechaRetiro,
        SuministroId = item.SuministroId, NumeroSuministro = item.Suministro.NumeroSuministro,
        NumeroContrato = item.Suministro.NumeroContrato, Cuen = item.Suministro.Cuen,
        TitularActual = $"{item.Suministro.ClienteTitular.Nombres} {item.Suministro.ClienteTitular.Apellidos}".Trim()
    };

    private static MedidorDetalleDto CrearDetalle(Medidor item) => new()
    {
        Id = item.Id, NumeroSerie = item.NumeroSerie, TipoMedicion = item.TipoMedicion,
        Estado = item.Estado, FechaInstalacion = item.FechaInstalacion, FechaRetiro = item.FechaRetiro,
        FechaRegistro = item.FechaRegistro, SuministroId = item.SuministroId,
        NumeroSuministro = item.Suministro.NumeroSuministro, NumeroContrato = item.Suministro.NumeroContrato,
        Cuen = item.Suministro.Cuen,
        TitularActual = $"{item.Suministro.ClienteTitular.Nombres} {item.Suministro.ClienteTitular.Apellidos}".Trim()
    };

    private static string CrearSnapshot(Medidor item) => JsonSerializer.Serialize(new
    {
        item.NumeroSerie, item.TipoMedicion, item.Estado, item.FechaInstalacion,
        item.FechaRetiro, Suministro = item.Suministro.NumeroSuministro
    });

    private static MedidorNormalizado Normalizar(CrearMedidorDto solicitud) => new(
        NormalizarTexto(solicitud.NumeroSerie).ToUpperInvariant(),
        NormalizarTexto(solicitud.NumeroSuministro).ToUpperInvariant(),
        NormalizarTexto(solicitud.TipoMedicion).ToUpperInvariant(),
        solicitud.FechaInstalacion.Date);

    private static string NormalizarTexto(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : Regex.Replace(valor.Trim(), "\\s+", " ");

    private ObjectResult ConflictoMedidorActivo() => Conflict(new
    {
        codigo = "SUMINISTRO_CON_MEDIDOR_ACTIVO",
        mensaje = "El suministro ya tiene un medidor activo. Retíralo o reemplázalo antes de continuar."
    });

    private ObjectResult ConflictoPersistencia(string accion) => Conflict(new
    {
        codigo = "MEDIDOR_NO_GUARDADO",
        mensaje = $"No fue posible {accion} el medidor porque sus datos entran en conflicto."
    });

    private sealed record MedidorNormalizado(
        string NumeroSerie, string NumeroSuministro, string TipoMedicion, DateTime FechaInstalacion);
}
