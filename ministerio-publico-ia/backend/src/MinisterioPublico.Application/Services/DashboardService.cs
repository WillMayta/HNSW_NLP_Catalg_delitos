using MinisterioPublico.Application.DTOs;
using MinisterioPublico.Application.Interfaces;
using MinisterioPublico.Domain.Interfaces;

namespace MinisterioPublico.Application.Services;

/// <summary>
/// Calcula los indicadores institucionales de calidad del dato que se
/// muestran en el Dashboard: cantidad de delitos, variantes detectadas,
/// delitos con mayor inconsistencia, fiscalías con mayor variabilidad,
/// evolución temporal y porcentaje de consolidación.
///
/// Nota de diseño (CQRS pragmático): las lecturas analíticas agregadas se
/// resuelven a través de IDashboardQueryRepository, un puerto de solo
/// lectura definido en el Dominio e implementado en Infraestructura con
/// consultas optimizadas (EF Core + LINQ -> SQL agregado). Esto evita que
/// la capa de Aplicación dependa de Infraestructura, preservando la regla
/// de dependencias de Clean Architecture (las flechas apuntan hacia adentro).
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IDashboardQueryRepository _consultas;

    public DashboardService(IDashboardQueryRepository consultas) => _consultas = consultas;

    public async Task<IndicadoresDashboardDto> ObtenerIndicadoresAsync(CancellationToken ct = default)
    {
        var totalOriginales = await _consultas.ContarDelitosOriginalesAsync(ct);
        var totalNormalizados = await _consultas.ContarDelitosNormalizadosAsync(ct);
        var totalConsolidados = await _consultas.ContarDelitosCatalogoConsolidadosAsync(ct);
        var propuestasPendientes = await _consultas.ContarPropuestasPendientesAsync(ct);
        var variantesAsociadas = await _consultas.ContarVariantesAsociadasAsync(ct);

        double porcentajeConsolidado = totalNormalizados == 0
            ? 0
            : Math.Round(100.0 * variantesAsociadas / totalNormalizados, 2);

        var delitosConMasInconsistencia = (await _consultas.ObtenerDelitosConMayorInconsistenciaAsync(10, ct))
            .Select(x => new DelitoConInconsistenciaDto(x.NombreGenerico, x.CantidadVariantes))
            .ToList();

        var fiscaliasConMasVariabilidad = (await _consultas.ObtenerFiscaliasConMayorVariabilidadAsync(10, ct))
            .Select(x => new FiscaliaVariabilidadDto(x.SiglaFiscalia, x.CantidadDenominacionesDistintas))
            .ToList();

        var evolucionCasos = await _consultas.ObtenerEvolucionTemporalCasosAsync(ct);
        var evolucionConsolidados = (await _consultas.ObtenerEvolucionTemporalConsolidadosAsync(ct))
            .ToDictionary(c => c.Periodo, c => c.Total);

        var evolucion = evolucionCasos
            .Select(e => new EvolucionTemporalDto(
                e.Periodo, e.Total, evolucionConsolidados.GetValueOrDefault(e.Periodo, 0)))
            .ToList();

        return new IndicadoresDashboardDto(
            TotalDelitosOriginales: totalOriginales,
            TotalVariantesDetectadas: totalNormalizados,
            TotalDelitosCatalogoConsolidados: totalConsolidados,
            PropuestasPendientesValidacion: propuestasPendientes,
            PorcentajeRegistrosConsolidados: porcentajeConsolidado,
            TiempoPromedioBusquedaMs: 0, // se completa con telemetría real en producción
            DelitosConMayorInconsistencia: delitosConMasInconsistencia,
            FiscaliasConMayorVariabilidad: fiscaliasConMasVariabilidad,
            EvolucionTemporal: evolucion);
    }
}
