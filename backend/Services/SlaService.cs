using Microsoft.EntityFrameworkCore;
using SistemaElectrico.Api.Data;
using SistemaElectrico.Api.Models;

namespace SistemaElectrico.Api.Services;

public sealed class SlaService(ApplicationDbContext context)
{
    private static readonly TimeSpan EcuadorOffset = TimeSpan.FromHours(-5);

    public async Task<DateTime?> CalcularLimiteAsync(Suministro suministro, string tipoOrden, DateTime inicioUtc)
    {
        var textoZona = $"{suministro.PlanGeocodigo} {suministro.NombreTarifa}";
        var zona = textoZona.Contains("RURAL", StringComparison.OrdinalIgnoreCase) ? "RURAL" : "URBANA";
        var politica = await context.PoliticasSla.AsNoTracking()
            .Where(x => x.Activa && x.TipoOrden == tipoOrden && (x.TipoZona == zona || x.TipoZona == "TODAS"))
            .OrderByDescending(x => x.TipoZona == zona).ThenByDescending(x => x.Version).FirstOrDefaultAsync();
        return politica is null ? null : Calcular(inicioUtc, politica);
    }

    private static DateTime Calcular(DateTime inicioUtc, PoliticaSla politica)
    {
        var local = DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc).Add(EcuadorOffset);
        local = AjustarInicio(local, politica);
        var restantes = TimeSpan.FromHours(politica.HorasObjetivo);
        while (restantes > TimeSpan.Zero)
        {
            var finJornada = local.Date + politica.HoraFinLaboral;
            var disponible = finJornada - local;
            if (restantes <= disponible) { local += restantes; break; }
            restantes -= disponible;
            local = SiguienteDiaLaborable(local.Date.AddDays(1), politica) + politica.HoraInicioLaboral;
        }
        return DateTime.SpecifyKind(local.Subtract(EcuadorOffset), DateTimeKind.Utc);
    }

    private static DateTime AjustarInicio(DateTime local, PoliticaSla politica)
    {
        var fecha = local.Date;
        if (!EsLaborable(fecha, politica)) return SiguienteDiaLaborable(fecha.AddDays(1), politica) + politica.HoraInicioLaboral;
        if (local.TimeOfDay < politica.HoraInicioLaboral) return fecha + politica.HoraInicioLaboral;
        if (local.TimeOfDay >= politica.HoraFinLaboral) return SiguienteDiaLaborable(fecha.AddDays(1), politica) + politica.HoraInicioLaboral;
        return local;
    }

    private static DateTime SiguienteDiaLaborable(DateTime fecha, PoliticaSla politica)
    {
        while (!EsLaborable(fecha, politica)) fecha = fecha.AddDays(1);
        return fecha;
    }

    private static bool EsLaborable(DateTime fecha, PoliticaSla politica) => politica.IncluyeFinDeSemana ||
        (fecha.DayOfWeek != DayOfWeek.Saturday && fecha.DayOfWeek != DayOfWeek.Sunday);
}
