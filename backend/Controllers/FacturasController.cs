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
public sealed class FacturasController : ControllerBase
{
    private static readonly string[] EstadosValidos =
        ["EMITIDA", "PARCIALMENTE_PAGADA", "PAGADA", "VENCIDA", "ANULADA"];

    private static readonly string[] MetodosValidos =
        ["REAL", "ESTIMADA", "PROMEDIO", "REMOTA", "AJUSTADA"];

    private static readonly string[] CategoriasValidas =
    [
        "ENERGIA", "COMERCIALIZACION", "ALUMBRADO_PUBLICO", "DESCUENTO", "SUBSIDIO",
        "INTERES_MORA", "SALDO_ANTERIOR", "TERCERO", "FINANCIAMIENTO", "CREDITO", "OTRO"
    ];

    private readonly ApplicationDbContext _context;

    public FacturasController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<FacturasPaginaDto>> Listar(
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 10, 50);
        var consulta = ConsultaCompleta().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(item =>
                item.NumeroFactura.Contains(termino) ||
                item.IdentificadorExterno.Contains(termino) ||
                item.Suministro.NumeroSuministro.Contains(termino) ||
                item.Suministro.NumeroContrato.Contains(termino) ||
                item.Suministro.Cuen.Contains(termino) ||
                item.Suministro.ClienteTitular.Nombres.Contains(termino) ||
                item.Suministro.ClienteTitular.Apellidos.Contains(termino));
        }

        var total = await consulta.CountAsync();
        var entidades = await consulta
            .OrderByDescending(item => item.FechaEmision)
            .ThenByDescending(item => item.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return Ok(new FacturasPaginaDto
        {
            Items = entidades.Select(CrearResumen).ToList(),
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FacturaDetalleDto>> ObtenerPorId(int id)
    {
        var factura = await ConsultaCompleta().AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);
        return factura is null ? NotFound() : Ok(CrearDetalle(factura));
    }

    [HttpPost("importar")]
    public async Task<ActionResult<ResultadoImportacionFacturaDto>> Importar(ImportarFacturaDto solicitud)
    {
        var datos = Normalizar(solicitud);
        Validar(datos);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var existente = await ConsultaCompleta().AsNoTracking().FirstOrDefaultAsync(item =>
            item.SistemaOrigen == datos.SistemaOrigen &&
            item.IdentificadorExterno == datos.IdentificadorExterno);
        if (existente is not null)
            return Ok(new ResultadoImportacionFacturaDto
            {
                Factura = CrearDetalle(existente),
                YaExistia = true
            });

        var suministro = await _context.Suministros
            .Include(item => item.ClienteTitular)
            .FirstOrDefaultAsync(item => item.NumeroSuministro == datos.NumeroSuministro);
        if (suministro is null)
            ModelState.AddModelError(nameof(ImportarFacturaDto.NumeroSuministro),
                "No existe un suministro con este número.");

        Medidor? medidor = null;
        if (!string.IsNullOrEmpty(datos.NumeroMedidor) && suministro is not null)
        {
            medidor = await _context.Medidores.FirstOrDefaultAsync(item =>
                item.NumeroSerie == datos.NumeroMedidor && item.SuministroId == suministro.Id);
            if (medidor is null)
                ModelState.AddModelError(nameof(ImportarFacturaDto.NumeroMedidor),
                    "El medidor no pertenece al suministro indicado.");
        }

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var factura = new Factura
        {
            Suministro = suministro!,
            Medidor = medidor,
            SistemaOrigen = datos.SistemaOrigen,
            IdentificadorExterno = datos.IdentificadorExterno,
            NumeroFactura = datos.NumeroFactura,
            NombreEmisor = datos.NombreEmisor,
            RucEmisor = datos.RucEmisor,
            FechaEmision = datos.FechaEmision,
            FechaVencimiento = datos.FechaVencimiento,
            Estado = datos.Estado,
            PeriodoDesde = datos.PeriodoDesde,
            PeriodoHasta = datos.PeriodoHasta,
            MetodoLectura = datos.MetodoLectura,
            LecturaAnterior = datos.LecturaAnterior,
            LecturaActual = datos.LecturaActual,
            ConsumoFacturado = datos.ConsumoFacturado,
            SubtotalServicioElectrico = datos.SubtotalServicioElectrico,
            SubtotalAlumbradoPublico = datos.SubtotalAlumbradoPublico,
            OtrosCargos = datos.OtrosCargos,
            SaldoAnterior = datos.SaldoAnterior,
            RecaudacionTerceros = datos.RecaudacionTerceros,
            Subsidios = datos.Subsidios,
            TotalFactura = datos.TotalFactura,
            SaldoPendiente = datos.SaldoPendiente,
            TieneDiferencia = Math.Abs(datos.Lineas.Sum(item => item.Monto) - datos.TotalFactura) > 0.01m,
            SincronizadaEnUtc = DateTime.UtcNow,
            Lineas = datos.Lineas.Select((linea, indice) => new FacturaLinea
            {
                NumeroLinea = indice + 1,
                CodigoOriginal = linea.CodigoOriginal,
                DescripcionOriginal = linea.DescripcionOriginal,
                Categoria = linea.Categoria,
                Monto = linea.Monto,
                EsIngresoElectrico = linea.EsIngresoElectrico
            }).ToList()
        };

        await using var transaccion = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Facturas.Add(factura);
            await _context.SaveChangesAsync();
            _context.EventosAuditoria.Add(new EventoAuditoria
            {
                ActorUsuarioId = ObtenerActorUsuarioId(),
                Accion = "FACTURA_IMPORTADA",
                TipoEntidad = nameof(Factura),
                EntidadId = factura.Id.ToString(),
                ValorNuevoJson = JsonSerializer.Serialize(new
                {
                    factura.SistemaOrigen,
                    factura.IdentificadorExterno,
                    factura.NumeroFactura,
                    Suministro = suministro!.NumeroSuministro,
                    factura.TotalFactura,
                    factura.SaldoPendiente,
                    factura.TieneDiferencia
                }),
                OcurridoEnUtc = DateTime.UtcNow,
                Motivo = "Importación desde el sistema comercial."
            });
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();
            _context.ChangeTracker.Clear();
            existente = await ConsultaCompleta().AsNoTracking().FirstOrDefaultAsync(item =>
                item.SistemaOrigen == datos.SistemaOrigen &&
                item.IdentificadorExterno == datos.IdentificadorExterno);
            if (existente is not null)
                return Ok(new ResultadoImportacionFacturaDto
                {
                    Factura = CrearDetalle(existente),
                    YaExistia = true
                });
            return Conflict(new
            {
                codigo = "FACTURA_NO_IMPORTADA",
                mensaje = "No fue posible importar la factura porque sus datos entran en conflicto."
            });
        }

        return CreatedAtAction(nameof(ObtenerPorId), new { id = factura.Id },
            new ResultadoImportacionFacturaDto
            {
                Factura = CrearDetalle(factura),
                YaExistia = false
            });
    }

    private IQueryable<Factura> ConsultaCompleta() => _context.Facturas
        .Include(item => item.Suministro).ThenInclude(item => item.ClienteTitular)
        .Include(item => item.Medidor)
        .Include(item => item.Lineas);

    private void Validar(FacturaNormalizada datos)
    {
        if (!Regex.IsMatch(datos.NumeroSuministro, "^[A-Z0-9-]{3,30}$"))
            ModelState.AddModelError(nameof(ImportarFacturaDto.NumeroSuministro),
                "Escribe un número de suministro válido.");
        if (!string.IsNullOrEmpty(datos.NumeroMedidor) &&
            !Regex.IsMatch(datos.NumeroMedidor, "^[A-Z0-9-]{3,50}$"))
            ModelState.AddModelError(nameof(ImportarFacturaDto.NumeroMedidor),
                "Escribe un número de medidor válido.");
        if (datos.SistemaOrigen.Length is < 2 or > 80)
            ModelState.AddModelError(nameof(ImportarFacturaDto.SistemaOrigen),
                "La fuente debe contener entre 2 y 80 caracteres.");
        if (datos.IdentificadorExterno.Length is < 3 or > 100)
            ModelState.AddModelError(nameof(ImportarFacturaDto.IdentificadorExterno),
                "El identificador externo debe contener entre 3 y 100 caracteres.");
        if (datos.NumeroFactura.Length is < 3 or > 60)
            ModelState.AddModelError(nameof(ImportarFacturaDto.NumeroFactura),
                "El número de factura debe contener entre 3 y 60 caracteres.");
        if (datos.NombreEmisor.Length is < 3 or > 160)
            ModelState.AddModelError(nameof(ImportarFacturaDto.NombreEmisor),
                "Escribe el nombre legal del emisor.");
        if (!Regex.IsMatch(datos.RucEmisor, "^[0-9]{13}$"))
            ModelState.AddModelError(nameof(ImportarFacturaDto.RucEmisor),
                "El RUC del emisor debe contener 13 dígitos.");
        if (!EstadosValidos.Contains(datos.Estado))
            ModelState.AddModelError(nameof(ImportarFacturaDto.Estado),
                "Selecciona un estado de factura válido.");
        if (!MetodosValidos.Contains(datos.MetodoLectura))
            ModelState.AddModelError(nameof(ImportarFacturaDto.MetodoLectura),
                "Selecciona un método de lectura válido.");
        if (datos.FechaEmision == default || datos.FechaEmision.Date > DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(ImportarFacturaDto.FechaEmision),
                "La fecha de emisión es obligatoria y no puede estar en el futuro.");
        if (datos.FechaVencimiento.Date < datos.FechaEmision.Date)
            ModelState.AddModelError(nameof(ImportarFacturaDto.FechaVencimiento),
                "La fecha de vencimiento no puede ser anterior a la emisión.");
        if (datos.PeriodoDesde == default || datos.PeriodoHasta.Date < datos.PeriodoDesde.Date)
            ModelState.AddModelError(nameof(ImportarFacturaDto.PeriodoHasta),
                "El período facturado no es válido.");
        if (datos.LecturaAnterior < 0 || datos.LecturaActual < 0 || datos.ConsumoFacturado < 0)
            ModelState.AddModelError(nameof(ImportarFacturaDto.ConsumoFacturado),
                "Las lecturas y el consumo no pueden ser negativos.");
        if (datos.TotalFactura < 0 || datos.SaldoPendiente < 0)
            ModelState.AddModelError(nameof(ImportarFacturaDto.TotalFactura),
                "El total y el saldo pendiente no pueden ser negativos.");
        if (datos.Lineas.Count is < 1 or > 100)
            ModelState.AddModelError(nameof(ImportarFacturaDto.Lineas),
                "La factura debe contener entre 1 y 100 rubros.");

        for (var indice = 0; indice < datos.Lineas.Count; indice++)
        {
            var linea = datos.Lineas[indice];
            if (linea.CodigoOriginal.Length is < 1 or > 50)
                ModelState.AddModelError($"Lineas[{indice}].CodigoOriginal", "Escribe el código original del rubro.");
            if (linea.DescripcionOriginal.Length is < 2 or > 200)
                ModelState.AddModelError($"Lineas[{indice}].DescripcionOriginal", "Escribe la descripción original del rubro.");
            if (!CategoriasValidas.Contains(linea.Categoria))
                ModelState.AddModelError($"Lineas[{indice}].Categoria", "Selecciona una categoría válida.");
        }
    }

    private int? ObtenerActorUsuarioId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static FacturaResumenDto CrearResumen(Factura factura) => new()
    {
        Id = factura.Id,
        NumeroFactura = factura.NumeroFactura,
        NumeroSuministro = factura.Suministro.NumeroSuministro,
        NumeroContrato = factura.Suministro.NumeroContrato,
        TitularActual = $"{factura.Suministro.ClienteTitular.Nombres} {factura.Suministro.ClienteTitular.Apellidos}".Trim(),
        FechaEmision = factura.FechaEmision,
        FechaVencimiento = factura.FechaVencimiento,
        PeriodoDesde = factura.PeriodoDesde,
        PeriodoHasta = factura.PeriodoHasta,
        ConsumoFacturado = factura.ConsumoFacturado,
        Estado = factura.Estado,
        TotalFactura = factura.TotalFactura,
        SaldoPendiente = factura.SaldoPendiente,
        TieneDiferencia = factura.TieneDiferencia
    };

    private static FacturaDetalleDto CrearDetalle(Factura factura) => new()
    {
        Id = factura.Id,
        NumeroFactura = factura.NumeroFactura,
        NumeroSuministro = factura.Suministro.NumeroSuministro,
        NumeroContrato = factura.Suministro.NumeroContrato,
        Cuen = factura.Suministro.Cuen,
        TitularActual = $"{factura.Suministro.ClienteTitular.Nombres} {factura.Suministro.ClienteTitular.Apellidos}".Trim(),
        NumeroMedidor = factura.Medidor?.NumeroSerie,
        SistemaOrigen = factura.SistemaOrigen,
        IdentificadorExterno = factura.IdentificadorExterno,
        NombreEmisor = factura.NombreEmisor,
        RucEmisor = factura.RucEmisor,
        FechaEmision = factura.FechaEmision,
        FechaVencimiento = factura.FechaVencimiento,
        PeriodoDesde = factura.PeriodoDesde,
        PeriodoHasta = factura.PeriodoHasta,
        Moneda = factura.Moneda,
        MetodoLectura = factura.MetodoLectura,
        LecturaAnterior = factura.LecturaAnterior,
        LecturaActual = factura.LecturaActual,
        ConsumoFacturado = factura.ConsumoFacturado,
        UnidadConsumo = factura.UnidadConsumo,
        Estado = factura.Estado,
        SubtotalServicioElectrico = factura.SubtotalServicioElectrico,
        SubtotalAlumbradoPublico = factura.SubtotalAlumbradoPublico,
        OtrosCargos = factura.OtrosCargos,
        SaldoAnterior = factura.SaldoAnterior,
        RecaudacionTerceros = factura.RecaudacionTerceros,
        Subsidios = factura.Subsidios,
        TotalFactura = factura.TotalFactura,
        SaldoPendiente = factura.SaldoPendiente,
        TieneDiferencia = factura.TieneDiferencia,
        SincronizadaEnUtc = factura.SincronizadaEnUtc,
        Lineas = factura.Lineas.OrderBy(linea => linea.NumeroLinea).Select(linea => new FacturaLineaDto
        {
            NumeroLinea = linea.NumeroLinea,
            CodigoOriginal = linea.CodigoOriginal,
            DescripcionOriginal = linea.DescripcionOriginal,
            Categoria = linea.Categoria,
            Monto = linea.Monto,
            EsIngresoElectrico = linea.EsIngresoElectrico
        }).ToList()
    };

    private static FacturaNormalizada Normalizar(ImportarFacturaDto solicitud) => new(
        NormalizarTexto(solicitud.NumeroSuministro).ToUpperInvariant(),
        NormalizarTexto(solicitud.NumeroMedidor).ToUpperInvariant(),
        NormalizarTexto(solicitud.SistemaOrigen).ToUpperInvariant(),
        NormalizarTexto(solicitud.IdentificadorExterno).ToUpperInvariant(),
        NormalizarTexto(solicitud.NumeroFactura).ToUpperInvariant(),
        NormalizarTexto(solicitud.NombreEmisor),
        Regex.Replace(solicitud.RucEmisor ?? string.Empty, "\\D", string.Empty),
        solicitud.FechaEmision.Date,
        solicitud.FechaVencimiento.Date,
        NormalizarTexto(solicitud.Estado).ToUpperInvariant(),
        solicitud.PeriodoDesde.Date,
        solicitud.PeriodoHasta.Date,
        NormalizarTexto(solicitud.MetodoLectura).ToUpperInvariant(),
        solicitud.LecturaAnterior,
        solicitud.LecturaActual,
        solicitud.ConsumoFacturado,
        solicitud.SubtotalServicioElectrico,
        solicitud.SubtotalAlumbradoPublico,
        solicitud.OtrosCargos,
        solicitud.SaldoAnterior,
        solicitud.RecaudacionTerceros,
        solicitud.Subsidios,
        solicitud.TotalFactura,
        solicitud.SaldoPendiente,
        (solicitud.Lineas ?? []).Select(linea => new FacturaLineaNormalizada(
            NormalizarTexto(linea.CodigoOriginal).ToUpperInvariant(),
            NormalizarTexto(linea.DescripcionOriginal),
            NormalizarTexto(linea.Categoria).ToUpperInvariant(),
            linea.Monto,
            linea.EsIngresoElectrico)).ToList());

    private static string NormalizarTexto(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : Regex.Replace(valor.Trim(), "\\s+", " ");

    private sealed record FacturaLineaNormalizada(
        string CodigoOriginal, string DescripcionOriginal, string Categoria,
        decimal Monto, bool EsIngresoElectrico);

    private sealed record FacturaNormalizada(
        string NumeroSuministro, string NumeroMedidor, string SistemaOrigen,
        string IdentificadorExterno, string NumeroFactura, string NombreEmisor,
        string RucEmisor, DateTime FechaEmision, DateTime FechaVencimiento,
        string Estado, DateTime PeriodoDesde, DateTime PeriodoHasta,
        string MetodoLectura, decimal LecturaAnterior, decimal LecturaActual,
        decimal ConsumoFacturado, decimal SubtotalServicioElectrico,
        decimal SubtotalAlumbradoPublico, decimal OtrosCargos, decimal SaldoAnterior,
        decimal RecaudacionTerceros, decimal Subsidios, decimal TotalFactura,
        decimal SaldoPendiente, List<FacturaLineaNormalizada> Lineas);
}
