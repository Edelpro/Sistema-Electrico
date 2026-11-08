using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.DTOs;
using SistemaElectrico.Api.Models;
using SistemaElectrico.Api.Services;
using System.Security.Cryptography;
using System.Text;

namespace SistemaElectrico.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthController(
        ApplicationDbContext context,
        TokenService tokenService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginDto loginDto)
    {
        var correo = loginDto.Correo.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u =>
                u.Correo == correo);

        // No revelar si el correo existe o si la cuenta fue desactivada.
        // El administrador puede consultar el estado desde el módulo de Usuarios.
        if (usuario == null || !usuario.Activo)
        {
            return Unauthorized(new
            {
                mensaje = "Correo o contraseña incorrectos."
            });
        }

        var passwordHasher = new PasswordHasher<Usuario>();

        var resultado = passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            loginDto.Password
        );

        if (resultado == PasswordVerificationResult.Failed)
        {
            usuario.IntentosFallidos++;

            await _context.SaveChangesAsync();

            return Unauthorized(new
            {
                mensaje = "Correo o contraseña incorrectos."
            });
        }

        usuario.IntentosFallidos = 0;
        usuario.UltimoAcceso = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var token = _tokenService.CrearToken(usuario);

        return Ok(new LoginResponseDto
        {
            UsuarioId = usuario.Id,
            NombreCompleto =
                $"{usuario.Nombres} {usuario.Apellidos}",
            Correo = usuario.Correo,
            Rol = usuario.Rol.Nombre,
            Token = token
        });
    }

    [HttpPost("solicitar-recuperacion")]
    [EnableRateLimiting("recuperacion-password")]
    public async Task<IActionResult> SolicitarRecuperacion(
        SolicitarRecuperacionPasswordDto dto,
        CancellationToken cancellationToken)
    {
        var correo = dto.Correo.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(
            item => item.Correo == correo && item.Activo,
            cancellationToken);

        if (usuario is not null)
        {
            var ahora = DateTime.UtcNow;
            var anteriores = await _context.TokensRecuperacionPassword
                .Where(item => item.UsuarioId == usuario.Id && item.UsadoEnUtc == null)
                .ToListAsync(cancellationToken);
            foreach (var anterior in anteriores) anterior.UsadoEnUtc = ahora;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            _context.TokensRecuperacionPassword.Add(new TokenRecuperacionPassword
            {
                UsuarioId = usuario.Id,
                TokenHash = HashToken(token),
                CreadoEnUtc = ahora,
                ExpiraEnUtc = ahora.AddMinutes(30)
            });
            await _context.SaveChangesAsync(cancellationToken);

            var webBase = (_configuration["App:WebBaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
            var fragmento = $"token={Uri.EscapeDataString(token)}&correo={Uri.EscapeDataString(correo)}";
            await _emailService.EnviarRecuperacionAsync(
                correo,
                usuario.Nombres,
                $"{webBase}/restablecer-contrasena#{fragmento}",
                cancellationToken);
        }

        return Accepted(new
        {
            mensaje = "Si el correo corresponde a una cuenta activa, enviaremos un enlace válido por 30 minutos."
        });
    }

    [HttpPost("restablecer-password")]
    [EnableRateLimiting("recuperacion-password")]
    public async Task<IActionResult> RestablecerPassword(
        RestablecerPasswordDto dto,
        CancellationToken cancellationToken)
    {
        var correo = dto.Correo.Trim().ToLowerInvariant();
        var hash = HashToken(dto.Token.Trim());
        var ahora = DateTime.UtcNow;
        var token = await _context.TokensRecuperacionPassword
            .Include(item => item.Usuario)
            .FirstOrDefaultAsync(item => item.TokenHash == hash &&
                item.Usuario.Correo == correo && item.Usuario.Activo,
                cancellationToken);

        if (token is null || token.UsadoEnUtc is not null || token.ExpiraEnUtc <= ahora)
        {
            return BadRequest(new { mensaje = "El enlace no es válido o ya venció. Solicita uno nuevo." });
        }

        var hasher = new PasswordHasher<Usuario>();
        token.Usuario.PasswordHash = hasher.HashPassword(token.Usuario, dto.NuevaPassword);
        token.Usuario.CredencialesActualizadasUtc = ahora;
        token.Usuario.IntentosFallidos = 0;
        token.Usuario.BloqueadoHasta = null;
        token.UsadoEnUtc = ahora;

        var otros = await _context.TokensRecuperacionPassword
            .Where(item => item.UsuarioId == token.UsuarioId && item.UsadoEnUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var otro in otros) otro.UsadoEnUtc = ahora;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { mensaje = "Contraseña actualizada. Ya puedes iniciar sesión." });
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
