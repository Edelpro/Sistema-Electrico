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
public sealed class SuministrosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SuministrosController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<SuministrosPaginaDto>> Listar(
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);

        var consulta = _context.Suministros.AsNoTracking()
            .Include(item => item.ClienteTitular).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(item =>
                item.NumeroSuministro.Contains(termino) ||
                item.NumeroContrato.Contains(termino) ||
                item.Cuen.Contains(termino) ||
                item.DireccionServicio.Contains(termino) ||
                item.ClienteTitular.Nombres.Contains(termino) ||
                item.ClienteTitular.Apellidos.Contains(termino));
        }

        var total = await consulta.CountAsync();
        var entidades = await consulta.OrderBy(item => item.NumeroSuministro)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync();
        return Ok(new SuministrosPaginaDto
        {
            Items = entidades.Select(CrearResumen).ToList(), Total = total,
            Pagina = pagina, TamanoPagina = tamanoPagina
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SuministroDetalleDto>> ObtenerPorId(int id)
    {
        var item = await _context.Suministros.AsNoTracking()
            .Include(suministro => suministro.ClienteTitular)
            .FirstOrDefaultAsync(suministro => suministro.Id == id);
        return item is null ? NotFound() : Ok(CrearDetalle(item));
    }

    [HttpPost]
    public async Task<ActionResult<SuministroDetalleDto>> Crear(CrearSuministroDto solicitud)
    {
        var datos = Normalizar(solicitud);
        Validar(datos);
        var titular = await BuscarTitularActivo(datos.ClienteIdentificacion);
        if (titular is null)
            ModelState.AddModelError(nameof(CrearSuministroDto.ClienteIdentificacion),
                "No existe un cliente activo con esta identificación.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var conflicto = await BuscarConflicto(datos.NumeroSuministro, datos.NumeroContrato, datos.Cuen);
        if (conflicto is not null) return conflicto;

        var suministro = CrearEntidad(datos, titular!);
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Suministros.Add(suministro);
            await _context.SaveChangesAsync();
            RegistrarAuditoria("SUMINISTRO_CREADO", suministro, null, CrearSnapshot(suministro),
                "Registro desde el portal administrativo.");
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("crear");
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = suministro.Id }, CrearDetalle(suministro));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SuministroDetalleDto>> Actualizar(int id, ActualizarSuministroDto solicitud)
    {
        var suministro = await _context.Suministros.Include(item => item.ClienteTitular)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (suministro is null) return NotFound();

        var datos = Normalizar(solicitud);
        Validar(datos);
        var motivo = NormalizarTexto(solicitud.Motivo);
        if (motivo.Length < 5)
            ModelState.AddModelError(nameof(ActualizarSuministroDto.Motivo),
                "Describe el motivo administrativo con al menos 5 caracteres.");
        var titular = await BuscarTitularActivo(datos.ClienteIdentificacion);
        if (titular is null)
            ModelState.AddModelError(nameof(CrearSuministroDto.ClienteIdentificacion),
                "No existe un cliente activo con esta identificación.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var conflicto = await BuscarConflicto(datos.NumeroSuministro, datos.NumeroContrato, datos.Cuen, id);
        if (conflicto is not null) return conflicto;

        var anterior = CrearSnapshot(suministro);
        AplicarDatos(suministro, datos, titular!);
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            RegistrarAuditoria("SUMINISTRO_ACTUALIZADO", suministro, anterior, CrearSnapshot(suministro), motivo);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("actualizar");
        }
        return Ok(CrearDetalle(suministro));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<SuministroDetalleDto>> CambiarEstado(int id, CambiarEstadoSuministroDto solicitud)
    {
        var suministro = await _context.Suministros.Include(item => item.ClienteTitular)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (suministro is null) return NotFound();
        var motivo = NormalizarTexto(solicitud.Motivo);
        if (motivo.Length < 5)
        {
            ModelState.AddModelError(nameof(CambiarEstadoSuministroDto.Motivo),
                "Describe el motivo con al menos 5 caracteres.");
            return ValidationProblem(ModelState);
        }

        var nuevoEstado = solicitud.Activo ? "ACTIVO" : "INACTIVO";
        if (suministro.EstadoServicio == nuevoEstado) return Ok(CrearDetalle(suministro));
        if (!solicitud.Activo && await _context.Medidores.AnyAsync(medidor =>
            medidor.SuministroId == id && medidor.Estado == "ACTIVO"))
            return Conflict(new
            {
                codigo = "SUMINISTRO_CON_MEDIDOR_ACTIVO",
                mensaje = "Retira o reemplaza el medidor activo antes de desactivar el suministro."
            });

        var anterior = CrearSnapshot(suministro);
        suministro.EstadoServicio = nuevoEstado;
        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            RegistrarAuditoria(solicitud.Activo ? "SUMINISTRO_REACTIVADO" : "SUMINISTRO_DESACTIVADO",
                suministro, anterior, CrearSnapshot(suministro), motivo);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia(solicitud.Activo ? "reactivar" : "desactivar");
        }
        return Ok(CrearDetalle(suministro));
    }

    private async Task<Cliente?> BuscarTitularActivo(string identificacion) =>
        await _context.Clientes.FirstOrDefaultAsync(item => item.Identificacion == identificacion && item.Activo);

    private async Task<ObjectResult?> BuscarConflicto(
        string suministro, string contrato, string cuen, int? excluirId = null)
    {
        var consulta = _context.Suministros.AsQueryable();
        if (excluirId.HasValue) consulta = consulta.Where(item => item.Id != excluirId.Value);
        if (await consulta.AnyAsync(item => item.NumeroSuministro == suministro))
            return Conflict(new { codigo = "SUMINISTRO_DUPLICADO", mensaje = "Ya existe este número de suministro." });
        if (await consulta.AnyAsync(item => item.NumeroContrato == contrato))
            return Conflict(new { codigo = "CONTRATO_DUPLICADO", mensaje = "Ya existe este número de contrato." });
        if (await consulta.AnyAsync(item => item.Cuen == cuen))
            return Conflict(new { codigo = "CUEN_DUPLICADO", mensaje = "Ya existe este CUEN." });
        return null;
    }

    private void Validar(SuministroNormalizado datos)
    {
        ValidarIdentificador(datos.NumeroSuministro, nameof(CrearSuministroDto.NumeroSuministro), 3, 30,
            "El suministro debe contener de 3 a 30 letras, números o guiones.");
        ValidarIdentificador(datos.NumeroContrato, nameof(CrearSuministroDto.NumeroContrato), 3, 30,
            "El contrato debe contener de 3 a 30 letras, números o guiones.");
        ValidarIdentificador(datos.Cuen, nameof(CrearSuministroDto.Cuen), 5, 50,
            "El CUEN debe contener de 5 a 50 letras, números o guiones.");
        if (datos.DireccionServicio.Length < 5)
            ModelState.AddModelError(nameof(CrearSuministroDto.DireccionServicio),
                "Ingresa una dirección de servicio válida.");
    }

    private void ValidarIdentificador(string valor, string campo, int minimo, int maximo, string mensaje)
    {
        if (!Regex.IsMatch(valor, $"^[A-Z0-9-]{{{minimo},{maximo}}}$"))
            ModelState.AddModelError(campo, mensaje);
    }

    private static Suministro CrearEntidad(SuministroNormalizado datos, Cliente titular)
    {
        var suministro = new Suministro { EstadoServicio = "ACTIVO", FechaRegistro = DateTime.UtcNow };
        AplicarDatos(suministro, datos, titular);
        return suministro;
    }

    private static void AplicarDatos(Suministro item, SuministroNormalizado datos, Cliente titular)
    {
        item.NumeroSuministro = datos.NumeroSuministro;
        item.NumeroContrato = datos.NumeroContrato;
        item.Cuen = datos.Cuen;
        item.ClienteTitularId = titular.Id;
        item.ClienteTitular = titular;
        item.DireccionServicio = datos.DireccionServicio;
        item.Provincia = datos.Provincia;
        item.Canton = datos.Canton;
        item.Parroquia = datos.Parroquia;
        item.PlanGeocodigo = datos.PlanGeocodigo;
        item.CodigoTarifa = datos.CodigoTarifa;
        item.NombreTarifa = datos.NombreTarifa;
    }

    private void RegistrarAuditoria(string accion, Suministro item, string? anterior, string nuevo, string motivo) =>
        _context.EventosAuditoria.Add(new EventoAuditoria
        {
            ActorUsuarioId = ObtenerActorUsuarioId(), Accion = accion, TipoEntidad = nameof(Suministro),
            EntidadId = item.Id.ToString(), ValorAnteriorJson = anterior, ValorNuevoJson = nuevo,
            OcurridoEnUtc = DateTime.UtcNow, Motivo = motivo
        });

    private int? ObtenerActorUsuarioId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static SuministroResumenDto CrearResumen(Suministro item) => new()
    {
        Id = item.Id, NumeroSuministro = item.NumeroSuministro, NumeroContrato = item.NumeroContrato,
        Cuen = item.Cuen, TitularActual = $"{item.ClienteTitular.Nombres} {item.ClienteTitular.Apellidos}".Trim(),
        DireccionServicio = item.DireccionServicio, Provincia = item.Provincia, Canton = item.Canton,
        Parroquia = item.Parroquia, PlanGeocodigo = item.PlanGeocodigo,
        CodigoTarifa = item.CodigoTarifa, NombreTarifa = item.NombreTarifa,
        EstadoServicio = item.EstadoServicio, FechaRegistro = item.FechaRegistro
    };

    private static SuministroDetalleDto CrearDetalle(Suministro item) => new()
    {
        Id = item.Id, NumeroSuministro = item.NumeroSuministro, NumeroContrato = item.NumeroContrato,
        Cuen = item.Cuen, ClienteTitularId = item.ClienteTitularId,
        ClienteIdentificacion = item.ClienteTitular.Identificacion,
        TitularActual = $"{item.ClienteTitular.Nombres} {item.ClienteTitular.Apellidos}".Trim(),
        DireccionServicio = item.DireccionServicio, Provincia = item.Provincia, Canton = item.Canton,
        Parroquia = item.Parroquia, PlanGeocodigo = item.PlanGeocodigo,
        CodigoTarifa = item.CodigoTarifa, NombreTarifa = item.NombreTarifa,
        EstadoServicio = item.EstadoServicio, FechaRegistro = item.FechaRegistro
    };

    private static string CrearSnapshot(Suministro item) => JsonSerializer.Serialize(new
    {
        item.NumeroSuministro, item.NumeroContrato, item.Cuen,
        ClienteIdentificacion = Enmascarar(item.ClienteTitular.Identificacion),
        item.DireccionServicio, item.Provincia, item.Canton, item.Parroquia,
        item.PlanGeocodigo, item.CodigoTarifa, item.NombreTarifa, item.EstadoServicio
    });

    private static SuministroNormalizado Normalizar(CrearSuministroDto solicitud) => new(
        NormalizarTexto(solicitud.NumeroSuministro).ToUpperInvariant(),
        NormalizarTexto(solicitud.NumeroContrato).ToUpperInvariant(),
        NormalizarTexto(solicitud.Cuen).ToUpperInvariant(),
        NormalizarTexto(solicitud.ClienteIdentificacion).ToUpperInvariant(),
        NormalizarTexto(solicitud.DireccionServicio), NormalizarOpcional(solicitud.Provincia),
        NormalizarOpcional(solicitud.Canton), NormalizarOpcional(solicitud.Parroquia),
        NormalizarOpcional(solicitud.PlanGeocodigo),
        NormalizarOpcional(solicitud.CodigoTarifa)?.ToUpperInvariant(),
        NormalizarOpcional(solicitud.NombreTarifa));

    private static string NormalizarTexto(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : Regex.Replace(valor.Trim(), "\\s+", " ");

    private static string? NormalizarOpcional(string? valor)
    {
        var resultado = NormalizarTexto(valor);
        return resultado.Length == 0 ? null : resultado;
    }

    private static string Enmascarar(string valor) => valor.Length <= 4
        ? new string('*', valor.Length)
        : $"{new string('*', valor.Length - 4)}{valor[^4..]}";

    private ObjectResult ConflictoPersistencia(string accion) => Conflict(new
    {
        codigo = "SUMINISTRO_NO_GUARDADO",
        mensaje = $"No fue posible {accion} el suministro porque sus datos entran en conflicto."
    });

    private sealed record SuministroNormalizado(
        string NumeroSuministro, string NumeroContrato, string Cuen, string ClienteIdentificacion,
        string DireccionServicio, string? Provincia, string? Canton, string? Parroquia,
        string? PlanGeocodigo, string? CodigoTarifa, string? NombreTarifa);
}
