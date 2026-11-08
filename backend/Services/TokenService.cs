using Microsoft.IdentityModel.Tokens;
using SistemaElectrico.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SistemaElectrico.Api.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CrearToken(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(
                ClaimTypes.Name,
                $"{usuario.Nombres} {usuario.Apellidos}"
            ),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Role, usuario.Rol.Nombre),
            new Claim("credenciales_actualizadas", (usuario.CredencialesActualizadasUtc?.Ticks ?? 0).ToString())
        };

        var clave = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new InvalidOperationException(
                "No se encontró la configuración Jwt:Key."
            );
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(clave)
        );

        var credenciales = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var expireMinutes =
            Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credenciales
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
