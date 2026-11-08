using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using SistemaElectrico.Api.Models;
using SistemaElectrico.Api.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SistemaElectrico.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public sealed class UsuariosController : ControllerBase
{
    private static readonly string[] RolesGestionables = ["Tecnico", "Usuario"];
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public UsuariosController(
        ApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<UsuariosPaginaDto>> Listar(
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);

        var consulta = _context.Usuarios
            .AsNoTracking()
            .Include(usuario => usuario.Rol)
            .Include(usuario => usuario.Cliente)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(usuario =>
                usuario.Nombres.Contains(termino) ||
                usuario.Apellidos.Contains(termino) ||
                usuario.Identificacion.Contains(termino) ||
                usuario.Correo.Contains(termino));
        }

        var total = await consulta.CountAsync();
        var entidades = await consulta
            .OrderBy(usuario => usuario.Apellidos)
            .ThenBy(usuario => usuario.Nombres)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return Ok(new UsuariosPaginaDto
        {
            Items = entidades.Select(CrearResumen).ToList(),
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDetalleDto>> ObtenerPorId(int id)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Include(item => item.Rol)
            .Include(item => item.Cliente)
            .FirstOrDefaultAsync(item => item.Id == id);

        return usuario is null ? NotFound() : Ok(CrearDetalle(usuario));
    }

    [HttpPost]
    public async Task<ActionResult<CrearUsuarioResultadoDto>> Crear(
        CrearUsuarioDto solicitud,
        CancellationToken cancellationToken)
    {
        var datos = Normalizar(solicitud);
        ValidarDatos(datos);

        var rol = await ObtenerRolGestionable(datos.Rol);
        if (rol is null)
        {
            ModelState.AddModelError(
                nameof(CrearUsuarioDto.Rol),
                "Selecciona el rol Técnico o Cliente / usuario.");
        }

        var cliente = await ObtenerClienteVinculado(datos.Rol, datos.IdentificacionCliente);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var conflicto = await BuscarConflicto(datos.Correo, datos.Identificacion);
        if (conflicto is not null)
        {
            return conflicto;
        }

        var usuario = new Usuario
        {
            Nombres = datos.Nombres,
            Apellidos = datos.Apellidos,
            Identificacion = datos.Identificacion,
            Correo = datos.Correo,
            Telefono = datos.Telefono,
            Activo = solicitud.Activo,
            FechaCreacion = DateTime.UtcNow,
            RolId = rol!.Id,
            Rol = rol,
            Cliente = cliente,
            ClienteId = cliente?.Id
        };

        var secretoInicial = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        usuario.PasswordHash = new PasswordHasher<Usuario>()
            .HashPassword(usuario, secretoInicial);

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            RegistrarAuditoria(
                "USUARIO_CREADO",
                usuario,
                valorAnterior: null,
                CrearSnapshot(usuario),
                $"Alta de cuenta con rol {EtiquetaRol(rol.Nombre)}.");

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("crear");
        }

        var invitacionEnviada = false;
        var mensaje = usuario.Activo
            ? "Cuenta creada. Puedes enviarle un enlace de acceso desde este módulo."
            : "Cuenta creada inactiva. Actívala antes de enviar el acceso.";

        if (usuario.Activo && solicitud.EnviarInvitacion)
        {
            invitacionEnviada = await CrearYEnviarInvitacion(usuario, cancellationToken);
            mensaje = invitacionEnviada
                ? $"Cuenta creada y enlace de acceso enviado a {usuario.Correo}."
                : "Cuenta creada, pero el correo no pudo enviarse. Revisa la configuración SMTP y usa «Enviar acceso» para reintentar.";
        }

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = usuario.Id },
            new CrearUsuarioResultadoDto
            {
                Usuario = CrearDetalle(usuario),
                InvitacionEnviada = invitacionEnviada,
                Mensaje = mensaje
            });
    }

    [HttpPost("{id:int}/invitacion")]
    public async Task<ActionResult<EnvioInvitacionUsuarioDto>> ReenviarInvitacion(
        int id,
        CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios
            .Include(item => item.Rol)
            .Include(item => item.Cliente)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (usuario is null)
        {
            return NotFound();
        }

        if (EsAdministrador(usuario))
        {
            return CuentaProtegida();
        }

        if (!usuario.Activo)
        {
            return Conflict(new
            {
                codigo = "CUENTA_INACTIVA",
                mensaje = "Activa la cuenta antes de enviar el enlace de acceso."
            });
        }

        var enviada = await CrearYEnviarInvitacion(usuario, cancellationToken);
        return Ok(new EnvioInvitacionUsuarioDto
        {
            InvitacionEnviada = enviada,
            Mensaje = enviada
                ? $"Enlace de acceso enviado a {usuario.Correo}."
                : "No se pudo enviar el correo. Revisa la configuración SMTP y vuelve a intentarlo."
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDetalleDto>> Actualizar(
        int id,
        ActualizarUsuarioDto solicitud)
    {
        var usuario = await _context.Usuarios
            .Include(item => item.Rol)
            .Include(item => item.Cliente)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (usuario is null)
        {
            return NotFound();
        }

        if (EsAdministrador(usuario))
        {
            return CuentaProtegida();
        }

        var datos = Normalizar(solicitud);
        ValidarDatos(datos);

        var motivo = NormalizarTexto(solicitud.Motivo);
        if (motivo.Length < 5)
        {
            ModelState.AddModelError(
                nameof(ActualizarUsuarioDto.Motivo),
                "Describe el motivo administrativo con al menos 5 caracteres.");
        }

        var rol = await ObtenerRolGestionable(datos.Rol);
        if (rol is null)
        {
            ModelState.AddModelError(
                nameof(ActualizarUsuarioDto.Rol),
                "Selecciona el rol Técnico o Cliente / usuario.");
        }


        var cliente = await ObtenerClienteVinculado(datos.Rol, datos.IdentificacionCliente, id);

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var conflicto = await BuscarConflicto(datos.Correo, datos.Identificacion, id);
        if (conflicto is not null)
        {
            return conflicto;
        }

        var valorAnterior = CrearSnapshot(usuario);
        usuario.Nombres = datos.Nombres;
        usuario.Apellidos = datos.Apellidos;
        usuario.Identificacion = datos.Identificacion;
        usuario.Correo = datos.Correo;
        usuario.Telefono = datos.Telefono;
        usuario.RolId = rol!.Id;
        usuario.Rol = rol;
        usuario.ClienteId = cliente?.Id;
        usuario.Cliente = cliente;

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            RegistrarAuditoria(
                "USUARIO_ACTUALIZADO",
                usuario,
                valorAnterior,
                CrearSnapshot(usuario),
                motivo);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia("actualizar");
        }

        return Ok(CrearDetalle(usuario));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<UsuarioDetalleDto>> CambiarEstado(
        int id,
        CambiarEstadoUsuarioDto solicitud)
    {
        var usuario = await _context.Usuarios
            .Include(item => item.Rol)
            .Include(item => item.Cliente)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (usuario is null)
        {
            return NotFound();
        }

        if (EsAdministrador(usuario))
        {
            return CuentaProtegida();
        }

        var motivo = NormalizarTexto(solicitud.Motivo);
        if (motivo.Length < 5)
        {
            ModelState.AddModelError(
                nameof(CambiarEstadoUsuarioDto.Motivo),
                "Describe el motivo con al menos 5 caracteres.");
            return ValidationProblem(ModelState);
        }

        if (usuario.Activo == solicitud.Activo)
        {
            return Ok(CrearDetalle(usuario));
        }

        var valorAnterior = CrearSnapshot(usuario);
        usuario.Activo = solicitud.Activo;

        await using var transaccion = await _context.Database.BeginTransactionAsync();

        try
        {
            RegistrarAuditoria(
                solicitud.Activo ? "USUARIO_REACTIVADO" : "USUARIO_DESACTIVADO",
                usuario,
                valorAnterior,
                CrearSnapshot(usuario),
                motivo);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            return ConflictoPersistencia(solicitud.Activo ? "reactivar" : "desactivar");
        }

        return Ok(CrearDetalle(usuario));
    }

    private async Task<Rol?> ObtenerRolGestionable(string codigo)
    {
        return await _context.Roles.FirstOrDefaultAsync(rol =>
            rol.Activo && RolesGestionables.Contains(rol.Nombre) && rol.Nombre == codigo);
    }

    private async Task<ObjectResult?> BuscarConflicto(
        string correo,
        string identificacion,
        int? excluirId = null)
    {
        var existeCorreo = await _context.Usuarios.AnyAsync(usuario =>
            usuario.Correo == correo &&
            (!excluirId.HasValue || usuario.Id != excluirId.Value));

        if (existeCorreo)
        {
            return Conflict(new
            {
                codigo = "CORREO_DUPLICADO",
                mensaje = "Ya existe una cuenta con este correo electrónico."
            });
        }

        var existeIdentificacion = await _context.Usuarios.AnyAsync(usuario =>
            usuario.Identificacion == identificacion &&
            (!excluirId.HasValue || usuario.Id != excluirId.Value));

        return existeIdentificacion
            ? Conflict(new
            {
                codigo = "IDENTIFICACION_DUPLICADA",
                mensaje = "Ya existe una cuenta con esta identificación."
            })
            : null;
    }

    private void ValidarDatos(UsuarioNormalizado datos)
    {
        if (datos.Nombres.Length == 0)
        {
            ModelState.AddModelError(nameof(CrearUsuarioDto.Nombres), "Ingresa los nombres.");
        }

        if (datos.Apellidos.Length == 0)
        {
            ModelState.AddModelError(nameof(CrearUsuarioDto.Apellidos), "Ingresa los apellidos.");
        }

        if (!Regex.IsMatch(datos.Identificacion, "^[0-9]{10}$"))
        {
            ModelState.AddModelError(
                nameof(CrearUsuarioDto.Identificacion),
                "La identificación debe contener exactamente 10 dígitos.");
        }

        if (datos.Telefono is not null &&
            !Regex.IsMatch(datos.Telefono, "^\\+?[0-9 ()-]{7,20}$"))
        {
            ModelState.AddModelError(
                nameof(CrearUsuarioDto.Telefono),
                "Escribe un teléfono válido de 7 a 20 caracteres.");
        }
    }

    private void RegistrarAuditoria(
        string accion,
        Usuario usuario,
        string? valorAnterior,
        string? valorNuevo,
        string motivo)
    {
        _context.EventosAuditoria.Add(new EventoAuditoria
        {
            ActorUsuarioId = ObtenerActorUsuarioId(),
            Accion = accion,
            TipoEntidad = nameof(Usuario),
            EntidadId = usuario.Id.ToString(),
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

    private static UsuarioResumenDto CrearResumen(Usuario usuario)
    {
        return new UsuarioResumenDto
        {
            Id = usuario.Id,
            NombreCompleto = $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
            Identificacion = EnmascararIdentificacion(usuario.Identificacion),
            Correo = usuario.Correo,
            Telefono = usuario.Telefono,
            Rol = usuario.Rol.Nombre,
            ClienteVinculado = NombreCliente(usuario.Cliente),
            Activo = usuario.Activo,
            UltimoAcceso = usuario.UltimoAcceso
        };
    }

    private static UsuarioDetalleDto CrearDetalle(Usuario usuario)
    {
        return new UsuarioDetalleDto
        {
            Id = usuario.Id,
            Nombres = usuario.Nombres,
            Apellidos = usuario.Apellidos,
            Identificacion = usuario.Identificacion,
            Correo = usuario.Correo,
            Telefono = usuario.Telefono,
            Rol = usuario.Rol.Nombre,
            IdentificacionCliente = usuario.Cliente?.Identificacion,
            ClienteVinculado = NombreCliente(usuario.Cliente),
            Activo = usuario.Activo,
            FechaCreacion = usuario.FechaCreacion,
            UltimoAcceso = usuario.UltimoAcceso
        };
    }

    private static string CrearSnapshot(Usuario usuario)
    {
        return JsonSerializer.Serialize(new
        {
            usuario.Nombres,
            usuario.Apellidos,
            Identificacion = EnmascararIdentificacion(usuario.Identificacion),
            usuario.Correo,
            usuario.Telefono,
            Rol = usuario.Rol.Nombre,
            Cliente = NombreCliente(usuario.Cliente),
            usuario.Activo
        });
    }

    private static UsuarioNormalizado Normalizar(CrearUsuarioDto solicitud)
    {
        return new UsuarioNormalizado(
            NormalizarTexto(solicitud.Nombres),
            NormalizarTexto(solicitud.Apellidos),
            NormalizarTexto(solicitud.Identificacion),
            NormalizarTexto(solicitud.Correo).ToLowerInvariant(),
            NormalizarOpcional(solicitud.Telefono),
            NormalizarRol(solicitud.Rol),
            NormalizarOpcional(solicitud.IdentificacionCliente));
    }

    private static UsuarioNormalizado Normalizar(ActualizarUsuarioDto solicitud)
    {
        return new UsuarioNormalizado(
            NormalizarTexto(solicitud.Nombres),
            NormalizarTexto(solicitud.Apellidos),
            NormalizarTexto(solicitud.Identificacion),
            NormalizarTexto(solicitud.Correo).ToLowerInvariant(),
            NormalizarOpcional(solicitud.Telefono),
            NormalizarRol(solicitud.Rol),
            NormalizarOpcional(solicitud.IdentificacionCliente));
    }

    private static string NormalizarRol(string? rol)
    {
        return NormalizarTexto(rol).ToUpperInvariant() switch
        {
            "TECNICO" or "TÉCNICO" => "Tecnico",
            "USUARIO" or "CLIENTE" => "Usuario",
            _ => string.Empty
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

    private static string EnmascararIdentificacion(string identificacion)
    {
        if (identificacion.Length <= 4)
        {
            return new string('*', identificacion.Length);
        }

        return $"{new string('*', identificacion.Length - 4)}{identificacion[^4..]}";
    }

    private static bool EsAdministrador(Usuario usuario)
    {
        return usuario.Rol.Nombre == "Administrador";
    }

    private async Task<Cliente?> ObtenerClienteVinculado(
        string rol,
        string? identificacion,
        int? excluirUsuarioId = null)
    {
        if (rol != "Usuario") return null;

        if (string.IsNullOrWhiteSpace(identificacion))
        {
            ModelState.AddModelError(
                nameof(CrearUsuarioDto.IdentificacionCliente),
                "Vincula la cuenta con la identificación de un cliente registrado.");
            return null;
        }

        var normalizada = identificacion.Trim().ToUpperInvariant();
        var cliente = await _context.Clientes.FirstOrDefaultAsync(item =>
            item.Identificacion.ToUpper() == normalizada && item.Activo);

        if (cliente is null)
        {
            ModelState.AddModelError(
                nameof(CrearUsuarioDto.IdentificacionCliente),
                "No existe un cliente activo con esa identificación.");
            return null;
        }

        var ocupado = await _context.Usuarios.AnyAsync(item =>
            item.ClienteId == cliente.Id &&
            (!excluirUsuarioId.HasValue || item.Id != excluirUsuarioId.Value));
        if (ocupado)
        {
            ModelState.AddModelError(
                nameof(CrearUsuarioDto.IdentificacionCliente),
                "Ese cliente ya está vinculado a otra cuenta de acceso.");
            return null;
        }

        return cliente;
    }

    private static string? NombreCliente(Cliente? cliente) => cliente is null
        ? null
        : $"{cliente.Nombres} {cliente.Apellidos}".Trim();

    private static string EtiquetaRol(string rol)
    {
        return rol == "Tecnico" ? "Técnico" : "Cliente / usuario";
    }

    private ObjectResult CuentaProtegida()
    {
        return Conflict(new
        {
            codigo = "CUENTA_ADMIN_PROTEGIDA",
            mensaje = "La cuenta administradora principal está protegida y no se modifica desde este módulo."
        });
    }

    private ObjectResult ConflictoPersistencia(string accion)
    {
        return Conflict(new
        {
            codigo = "USUARIO_NO_GUARDADO",
            mensaje = $"No fue posible {accion} la cuenta porque sus datos entran en conflicto con otro registro."
        });
    }

    private async Task<bool> CrearYEnviarInvitacion(
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        var ahora = DateTime.UtcNow;
        var anteriores = await _context.TokensRecuperacionPassword
            .Where(item => item.UsuarioId == usuario.Id && item.UsadoEnUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var anterior in anteriores)
        {
            anterior.UsadoEnUtc = ahora;
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        _context.TokensRecuperacionPassword.Add(new TokenRecuperacionPassword
        {
            UsuarioId = usuario.Id,
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))),
            CreadoEnUtc = ahora,
            ExpiraEnUtc = ahora.AddMinutes(30)
        });

        RegistrarAuditoria(
            "INVITACION_USUARIO_SOLICITADA",
            usuario,
            valorAnterior: null,
            valorNuevo: JsonSerializer.Serialize(new { usuario.Correo, ExpiraEnUtc = ahora.AddMinutes(30) }),
            motivo: "Envío de enlace para establecer la contraseña de acceso.");
        await _context.SaveChangesAsync(cancellationToken);

        var webBase = (_configuration["App:WebBaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
        var fragmento = $"token={Uri.EscapeDataString(token)}&correo={Uri.EscapeDataString(usuario.Correo)}";
        return await _emailService.EnviarInvitacionAsync(
            usuario.Correo,
            usuario.Nombres,
            $"{webBase}/restablecer-contrasena#{fragmento}",
            cancellationToken);
    }

    private sealed record UsuarioNormalizado(
        string Nombres,
        string Apellidos,
        string Identificacion,
        string Correo,
        string? Telefono,
        string Rol,
        string? IdentificacionCliente);
}
