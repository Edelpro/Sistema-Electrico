using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.Models;
using SistemaElectrico.Api.Services;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// CONTROLADORES
builder.Services.AddControllers();

// BASE DE DATOS
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// SERVICIOS
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SlaService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("recuperacion-password", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "local",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// AUTENTICACIÓN JWT
// La clave nunca se guarda en appsettings.json. En desarrollo se toma de
// appsettings.Development.json; en producción, de la variable de entorno Jwt__Key.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:Key con al menos 32 caracteres (variable de entorno Jwt__Key en producción).");
}
if (!builder.Environment.IsDevelopment() &&
    jwtKey.Contains("DESARROLLO", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "La clave JWT de desarrollo no puede usarse fuera del entorno Development.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var valorUsuarioId = context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(valorUsuarioId, out var usuarioId))
                {
                    context.Fail("La sesión no contiene una identidad válida.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>();
                var cuenta = await db.Usuarios
                    .AsNoTracking()
                    .Where(usuario => usuario.Id == usuarioId)
                    .Select(usuario => new { usuario.Activo, usuario.CredencialesActualizadasUtc })
                    .FirstOrDefaultAsync();

                if (cuenta is null || !cuenta.Activo)
                {
                    context.Fail("La cuenta se encuentra inactiva.");
                    return;
                }

                var versionClaim = context.Principal?.FindFirstValue("credenciales_actualizadas");
                if (!long.TryParse(versionClaim, out var version) ||
                    version != (cuenta.CredencialesActualizadasUtc?.Ticks ?? 0))
                {
                    context.Fail("La sesión fue invalidada por un cambio de credenciales.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// CORS PARA ANGULAR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// SWAGGER SOLO EN DESARROLLO
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// En desarrollo Angular consume el puerto HTTP local para evitar depender del
// certificado HTTPS de desarrollo. Producción conserva la redirección segura.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AngularApp");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// CREACIÓN DEL ADMINISTRADOR INICIAL
using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var existeAdmin =
        context.Usuarios.Any(
            u => u.Correo == "admin@sistemaelectrico.ec");

    if (!existeAdmin)
    {
        var administrador = new Usuario
        {
            Nombres = "Administrador",
            Apellidos = "Sistema",
            Identificacion = "0000000000",
            Correo = "admin@sistemaelectrico.ec",
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            RolId = 1
        };

        var passwordHasher = new PasswordHasher<Usuario>();

        administrador.PasswordHash =
            passwordHasher.HashPassword(
                administrador,
                "Admin2026*"
            );

        context.Usuarios.Add(administrador);
        context.SaveChanges();
    }
}

app.Run();
