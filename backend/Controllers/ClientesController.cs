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
public sealed class ClientesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClientesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ClientesPaginaDto>> Listar(
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);

        var consulta = _context.Clientes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(cliente =>
                cliente.Nombres.Contains(termino) ||
                cliente.Apellidos.Contains(termino) ||
                cliente.Identificacion.Contains(termino) ||
                (cliente.Correo != null && cliente.Correo.Contains(termino)) ||
                (cliente.Telefono != null && cliente.Telefono.Contains(termino)));
        }

        var total = await consulta.CountAsync();
        var entidades = await consulta
            .OrderBy(cliente => cliente.Apellidos)
            .ThenBy(cliente => cliente.Nombres)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return Ok(new ClientesPaginaDto
        {
            Items = entidades.Select(CrearResumen).ToList(),
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClienteDetalleDto>> ObtenerPorId(int id)
    {
        var cliente = await _context.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return cliente is null ? NotFound() : Ok(CrearDetalle(cliente));
    }

    [HttpPost]
    public async Task<ActionResult<ClienteDetalleDto>> Crear(CrearClienteDto solicitud)
    {
        var datos = Normalizar(solicitud);
        ValidarSolicitud(datos);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (await ExisteIdentificacion(datos.Identificacion))
        {
            return ConflictoIdentificacion();
        }

        var cliente = new Cliente
        {
            TipoIdentificacion = NombreTipoIdentificacion(datos.TipoIdentificacion),
            Identificacion = datos.Identificacion,
            Nombres = datos.Nombres,
            Apellidos = datos.Apellidos,
            Correo = datos.Correo,
            Telefono = datos.Telefono,
            DireccionNotificacion = datos.DireccionNotificacion,
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            RegistrarAuditoria(
                "CLIENTE_CREADO",
                cliente,
                valorAnterior: null,
                valorNuevo: CrearSnapshot(cliente),
                "Registro desde el portal administrativo.");

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("crear");
        }

        var respuesta = CrearDetalle(cliente);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.Id }, respuesta);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClienteDetalleDto>> Actualizar(
        int id,
        CrearClienteDto solicitud)
    {
        var cliente = await _context.Clientes.FirstOrDefaultAsync(item => item.Id == id);

        if (cliente is null)
        {
            return NotFound();
        }

        var datos = Normalizar(solicitud);
        ValidarSolicitud(datos);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (await ExisteIdentificacion(datos.Identificacion, id))
        {
            return ConflictoIdentificacion();
        }

        var valorAnterior = CrearSnapshot(cliente);
        cliente.TipoIdentificacion = NombreTipoIdentificacion(datos.TipoIdentificacion);
        cliente.Identificacion = datos.Identificacion;
        cliente.Nombres = datos.Nombres;
        cliente.Apellidos = datos.Apellidos;
        cliente.Correo = datos.Correo;
        cliente.Telefono = datos.Telefono;
        cliente.DireccionNotificacion = datos.DireccionNotificacion;

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            RegistrarAuditoria(
                "CLIENTE_ACTUALIZADO",
                cliente,
                valorAnterior,
                CrearSnapshot(cliente),
                "Actualización desde el portal administrativo.");

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("actualizar");
        }

        return Ok(CrearDetalle(cliente));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<ClienteDetalleDto>> CambiarEstado(
        int id,
        CambiarEstadoClienteDto solicitud)
    {
        var cliente = await _context.Clientes.FirstOrDefaultAsync(item => item.Id == id);

        if (cliente is null)
        {
            return NotFound();
        }

        var motivo = NormalizarTexto(solicitud.Motivo);
        if (motivo.Length < 5)
        {
            ModelState.AddModelError(
                nameof(CambiarEstadoClienteDto.Motivo),
                "Describe el motivo con al menos 5 caracteres.");
            return ValidationProblem(ModelState);
        }

        if (cliente.Activo == solicitud.Activo)
        {
            return Ok(CrearDetalle(cliente));
        }

        if (!solicitud.Activo)
        {
            var tieneSuministroActivo = await _context.Suministros.AnyAsync(suministro =>
                suministro.ClienteTitularId == cliente.Id &&
                suministro.EstadoServicio.ToUpper() == "ACTIVO");

            if (tieneSuministroActivo)
            {
                return Conflict(new
                {
                    codigo = "CLIENTE_CON_SUMINISTRO_ACTIVO",
                    mensaje = "No se puede desactivar al cliente mientras figure como titular de un suministro activo."
                });
            }
        }

        var valorAnterior = CrearSnapshot(cliente);
        cliente.Activo = solicitud.Activo;

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            RegistrarAuditoria(
                solicitud.Activo ? "CLIENTE_REACTIVADO" : "CLIENTE_DESACTIVADO",
                cliente,
                valorAnterior,
                CrearSnapshot(cliente),
                motivo);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia(solicitud.Activo ? "reactivar" : "desactivar");
        }

        return Ok(CrearDetalle(cliente));
    }

    private async Task<bool> ExisteIdentificacion(string identificacion, int? excluirId = null)
    {
        return await _context.Clientes.AnyAsync(cliente =>
            cliente.Identificacion == identificacion &&
            (!excluirId.HasValue || cliente.Id != excluirId.Value));
    }

    private void ValidarSolicitud(ClienteNormalizado datos)
    {
        if (datos.TipoIdentificacion is not ("CEDULA" or "RUC" or "PASAPORTE"))
        {
            ModelState.AddModelError(
                nameof(CrearClienteDto.TipoIdentificacion),
                "Selecciona un tipo de identificación válido.");
        }

        if (datos.TipoIdentificacion == "CEDULA" && !Regex.IsMatch(datos.Identificacion, "^[0-9]{10}$"))
        {
            ModelState.AddModelError(
                nameof(CrearClienteDto.Identificacion),
                "La cédula debe contener exactamente 10 dígitos.");
        }
        else if (datos.TipoIdentificacion == "RUC" && !Regex.IsMatch(datos.Identificacion, "^[0-9]{13}$"))
        {
            ModelState.AddModelError(
                nameof(CrearClienteDto.Identificacion),
                "El RUC debe contener exactamente 13 dígitos.");
        }
        else if (datos.TipoIdentificacion == "PASAPORTE" && !Regex.IsMatch(datos.Identificacion, "^[A-Z0-9-]{5,20}$"))
        {
            ModelState.AddModelError(
                nameof(CrearClienteDto.Identificacion),
                "El pasaporte debe contener entre 5 y 20 letras, números o guiones.");
        }

        if (datos.Nombres.Length == 0)
        {
            ModelState.AddModelError(
                nameof(CrearClienteDto.Nombres),
                "Ingresa los nombres o la razón social.");
        }

        if (datos.TipoIdentificacion != "RUC" && datos.Apellidos.Length == 0)
        {
            ModelState.AddModelError(
                nameof(CrearClienteDto.Apellidos),
                "Ingresa los apellidos de la persona.");
        }

        if (datos.Telefono is not null &&
            !Regex.IsMatch(datos.Telefono, "^\\+?[0-9 ()-]{7,20}$"))
        {
            ModelState.AddModelError(
                nameof(CrearClienteDto.Telefono),
                "Escribe un teléfono válido de 7 a 20 caracteres.");
        }
    }

    private void RegistrarAuditoria(
        string accion,
        Cliente cliente,
        string? valorAnterior,
        string? valorNuevo,
        string motivo)
    {
        _context.EventosAuditoria.Add(new EventoAuditoria
        {
            ActorUsuarioId = ObtenerActorUsuarioId(),
            Accion = accion,
            TipoEntidad = nameof(Cliente),
            EntidadId = cliente.Id.ToString(),
            ValorAnteriorJson = valorAnterior,
            ValorNuevoJson = valorNuevo,
            OcurridoEnUtc = DateTime.UtcNow,
            Motivo = motivo
        });
    }

    private int? ObtenerActorUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, out var id) ? id : null;
    }

    private static ClienteResumenDto CrearResumen(Cliente cliente)
    {
        return new ClienteResumenDto
        {
            Id = cliente.Id,
            NombreCompleto = $"{cliente.Nombres} {cliente.Apellidos}".Trim(),
            TipoIdentificacion = cliente.TipoIdentificacion,
            Identificacion = EnmascararIdentificacion(cliente.Identificacion),
            Correo = cliente.Correo,
            Telefono = cliente.Telefono,
            DireccionNotificacion = cliente.DireccionNotificacion,
            Activo = cliente.Activo,
            FechaRegistro = cliente.FechaRegistro
        };
    }

    private static ClienteDetalleDto CrearDetalle(Cliente cliente)
    {
        return new ClienteDetalleDto
        {
            Id = cliente.Id,
            TipoIdentificacion = CodigoTipoIdentificacion(cliente.TipoIdentificacion),
            Identificacion = cliente.Identificacion,
            Nombres = cliente.Nombres,
            Apellidos = cliente.Apellidos,
            Correo = cliente.Correo,
            Telefono = cliente.Telefono,
            DireccionNotificacion = cliente.DireccionNotificacion,
            Activo = cliente.Activo,
            FechaRegistro = cliente.FechaRegistro
        };
    }

    private static string CrearSnapshot(Cliente cliente)
    {
        return JsonSerializer.Serialize(new
        {
            cliente.TipoIdentificacion,
            Identificacion = EnmascararIdentificacion(cliente.Identificacion),
            cliente.Nombres,
            cliente.Apellidos,
            cliente.Correo,
            cliente.Telefono,
            cliente.DireccionNotificacion,
            cliente.Activo
        });
    }

    private static ClienteNormalizado Normalizar(CrearClienteDto solicitud)
    {
        return new ClienteNormalizado(
            NormalizarTexto(solicitud.TipoIdentificacion).ToUpperInvariant(),
            NormalizarTexto(solicitud.Identificacion).ToUpperInvariant(),
            NormalizarTexto(solicitud.Nombres),
            NormalizarTexto(solicitud.Apellidos),
            NormalizarOpcional(solicitud.Correo)?.ToLowerInvariant(),
            NormalizarOpcional(solicitud.Telefono),
            NormalizarOpcional(solicitud.DireccionNotificacion));
    }

    private static string EnmascararIdentificacion(string identificacion)
    {
        if (identificacion.Length <= 4)
        {
            return new string('*', identificacion.Length);
        }

        return $"{new string('*', identificacion.Length - 4)}{identificacion[^4..]}";
    }

    private static string NombreTipoIdentificacion(string tipo)
    {
        return tipo switch
        {
            "CEDULA" => "Cédula",
            "RUC" => "RUC",
            _ => "Pasaporte"
        };
    }

    private static string CodigoTipoIdentificacion(string tipo)
    {
        return tipo.ToUpperInvariant() switch
        {
            "CÉDULA" or "CEDULA" => "CEDULA",
            "RUC" => "RUC",
            _ => "PASAPORTE"
        };
    }

    private static string NormalizarTexto(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? string.Empty
            : Regex.Replace(valor.Trim(), "\\s+", " ");
    }

    private static string? NormalizarOpcional(string? valor)
    {
        var normalizado = NormalizarTexto(valor);
        return normalizado.Length == 0 ? null : normalizado;
    }

    private ObjectResult ConflictoIdentificacion()
    {
        return Conflict(new
        {
            codigo = "IDENTIFICACION_DUPLICADA",
            mensaje = "Ya existe un cliente con esta identificación."
        });
    }

    private ObjectResult ConflictoPersistencia(string accion)
    {
        return Conflict(new
        {
            codigo = "CLIENTE_NO_GUARDADO",
            mensaje = $"No fue posible {accion} el cliente porque sus datos entran en conflicto con otro registro."
        });
    }

    private sealed record ClienteNormalizado(
        string TipoIdentificacion,
        string Identificacion,
        string Nombres,
        string Apellidos,
        string? Correo,
        string? Telefono,
        string? DireccionNotificacion);
}
