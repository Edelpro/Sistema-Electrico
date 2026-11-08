using System.ComponentModel.DataAnnotations;

namespace SistemaElectrico.Api.DTOs;

public sealed class GuardarCuadrillaDto
{
    [Required, RegularExpression("^[A-Za-z0-9-]{3,30}$")] public string Codigo { get; set; } = string.Empty;
    [Required, MinLength(3), MaxLength(120)] public string Nombre { get; set; } = string.Empty;
    [Required, MinLength(2), MaxLength(120)] public string Zona { get; set; } = string.Empty;
    [Range(1, 20)] public int CapacidadMaxima { get; set; } = 1;
    [Required, MaxLength(30)] public string Estado { get; set; } = "DISPONIBLE";
    [MaxLength(500)] public string? Observaciones { get; set; }
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
}

public sealed class CambiarEstadoCuadrillaDto
{
    [Required, MaxLength(30)] public string Estado { get; set; } = string.Empty;
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
}

public sealed class AgregarMiembroCuadrillaDto
{
    [Required, EmailAddress, MaxLength(160)] public string CorreoTecnico { get; set; } = string.Empty;
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
}

public sealed class RetirarMiembroCuadrillaDto
{
    [Required, MinLength(5), MaxLength(300)] public string Motivo { get; set; } = string.Empty;
}

public sealed record CuadrillaMiembroDto(int Id, int UsuarioId, string Nombre, string Correo, DateTime FechaAsignacionUtc);
public sealed record CuadrillaResumenDto(int Id, string Codigo, string Nombre, string Zona, int CapacidadMaxima,
    int MiembrosActivos, string Estado, bool Activa, int OrdenesActivas);
public sealed record CuadrillaDetalleDto(int Id, string Codigo, string Nombre, string Zona, int CapacidadMaxima,
    string Estado, bool Activa, string? Observaciones, DateTime FechaCreacionUtc, DateTime FechaActualizacionUtc,
    IReadOnlyList<CuadrillaMiembroDto> Miembros, int OrdenesActivas);
public sealed record CuadrillasPaginaDto(IReadOnlyList<CuadrillaResumenDto> Items, int Total, int Pagina, int TamanoPagina);
