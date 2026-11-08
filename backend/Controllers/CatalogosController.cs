using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;

namespace SistemaElectrico.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public sealed class CatalogosController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("asignacion-operativa")]
    public async Task<IActionResult> AsignacionOperativa(CancellationToken cancellationToken)
    {
        var clientes = await context.Clientes.AsNoTracking()
            .Where(item => item.Activo)
            .OrderBy(item => item.Apellidos).ThenBy(item => item.Nombres)
            .Select(item => new
            {
                item.Id,
                item.Identificacion,
                item.TipoIdentificacion,
                Nombre = (item.Nombres + " " + item.Apellidos).Trim()
            })
            .ToListAsync(cancellationToken);

        var tecnicos = await context.Usuarios.AsNoTracking()
            .Where(item => item.Activo && item.Rol.Nombre == "Tecnico")
            .OrderBy(item => item.Apellidos).ThenBy(item => item.Nombres)
            .Select(item => new
            {
                item.Id,
                Nombre = (item.Nombres + " " + item.Apellidos).Trim(),
                item.Correo
            })
            .ToListAsync(cancellationToken);

        var administradores = await context.Usuarios.AsNoTracking()
            .Where(item => item.Activo && item.Rol.Nombre == "Administrador")
            .OrderBy(item => item.Apellidos).ThenBy(item => item.Nombres)
            .Select(item => new
            {
                item.Id,
                Nombre = (item.Nombres + " " + item.Apellidos).Trim(),
                item.Correo
            })
            .ToListAsync(cancellationToken);

        var cuadrillas = await context.Cuadrillas.AsNoTracking()
            .Where(item => item.Estado != "INACTIVA")
            .OrderBy(item => item.Codigo)
            .Select(item => new { item.Id, item.Codigo, item.Nombre, item.Zona, item.Estado })
            .ToListAsync(cancellationToken);

        var suministros = await context.Suministros.AsNoTracking()
            .Where(item => item.EstadoServicio == "ACTIVO")
            .OrderBy(item => item.NumeroSuministro)
            .Select(item => new
            {
                item.Id,
                item.NumeroSuministro,
                Titular = (item.ClienteTitular.Nombres + " " + item.ClienteTitular.Apellidos).Trim(),
                item.DireccionServicio
            })
            .ToListAsync(cancellationToken);

        var medidores = await context.Medidores.AsNoTracking()
            .Where(item => item.Estado == "ACTIVO" && item.Suministro.EstadoServicio == "ACTIVO")
            .OrderBy(item => item.NumeroSerie)
            .Select(item => new
            {
                item.Id,
                item.NumeroSerie,
                item.TipoMedicion,
                item.Suministro.NumeroSuministro
            })
            .ToListAsync(cancellationToken);

        var facturas = await context.Facturas.AsNoTracking()
            .Where(item => item.SaldoPendiente > 0)
            .OrderByDescending(item => item.FechaEmision)
            .Select(item => new
            {
                item.Id,
                item.NumeroFactura,
                item.Suministro.NumeroSuministro,
                item.SaldoPendiente,
                item.Estado
            })
            .ToListAsync(cancellationToken);

        var pagos = await context.Pagos.AsNoTracking()
            .OrderByDescending(item => item.FechaConfirmacionUtc)
            .Select(item => new
            {
                item.Id,
                item.Referencia,
                item.Suministro.NumeroSuministro,
                item.Monto,
                item.Estado
            })
            .ToListAsync(cancellationToken);

        var ordenes = await context.OrdenesTrabajo.AsNoTracking()
            .OrderByDescending(item => item.FechaCreacionUtc)
            .Select(item => new
            {
                item.Id,
                item.Codigo,
                item.Suministro.NumeroSuministro,
                item.Tipo,
                item.Estado
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            clientes,
            tecnicos,
            administradores,
            cuadrillas,
            suministros,
            medidores,
            facturas,
            pagos,
            ordenes
        });
    }
}
