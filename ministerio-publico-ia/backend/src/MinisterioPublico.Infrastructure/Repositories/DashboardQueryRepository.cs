using Microsoft.EntityFrameworkCore;
using MinisterioPublico.Domain.Entities;
using MinisterioPublico.Domain.Interfaces;
using MinisterioPublico.Infrastructure.Persistence;

namespace MinisterioPublico.Infrastructure.Repositories;

public class DashboardQueryRepository : IDashboardQueryRepository
{
    private readonly MinisterioPublicoDbContext _db;
    public DashboardQueryRepository(MinisterioPublicoDbContext db) => _db = db;

    public Task<int> ContarDelitosOriginalesAsync(CancellationToken ct = default)
        => _db.DelitosOriginales.CountAsync(ct);

    public Task<int> ContarDelitosNormalizadosAsync(CancellationToken ct = default)
        => _db.DelitosNormalizados.CountAsync(ct);

    public Task<int> ContarDelitosCatalogoConsolidadosAsync(CancellationToken ct = default)
        => _db.DelitosCatalogo.CountAsync(
            d => d.Estado == EstadoCatalogo.Validado || d.Estado == EstadoCatalogo.Consolidado, ct);

    public Task<int> ContarPropuestasPendientesAsync(CancellationToken ct = default)
        => _db.PropuestasAgrupamiento.CountAsync(p => p.Estado == EstadoPropuesta.PendienteValidacion, ct);

    public Task<int> ContarVariantesAsociadasAsync(CancellationToken ct = default)
        => _db.DelitosNormalizados.CountAsync(n => n.VarianteDeId != null, ct);

    public async Task<IReadOnlyList<(string NombreGenerico, int CantidadVariantes)>> ObtenerDelitosConMayorInconsistenciaAsync(
        int top, CancellationToken ct = default)
    {
        var datos = await _db.DelitosCatalogo
            .OrderByDescending(d => d.Variantes.Count)
            .Take(top)
            .Select(d => new { d.NombreGenerico, Cantidad = d.Variantes.Count })
            .ToListAsync(ct);
        return datos.Select(x => (x.NombreGenerico, x.Cantidad)).ToList();
    }

    public async Task<IReadOnlyList<(string SiglaFiscalia, int CantidadDenominacionesDistintas)>> ObtenerFiscaliasConMayorVariabilidadAsync(
        int top, CancellationToken ct = default)
    {
        var datos = await _db.DelitosOriginales
            .GroupBy(d => d.SiglaFiscalia)
            .Select(g => new { Sigla = g.Key, Cantidad = g.Select(x => x.TextoOriginal).Distinct().Count() })
            .OrderByDescending(x => x.Cantidad)
            .Take(top)
            .ToListAsync(ct);
        return datos.Select(x => (x.Sigla, x.Cantidad)).ToList();
    }

    public async Task<IReadOnlyList<(string Periodo, int Total)>> ObtenerEvolucionTemporalCasosAsync(CancellationToken ct = default)
    {
        var datos = await _db.DelitosOriginales
            .GroupBy(d => new { d.FechaIngresoCaso.Year, d.FechaIngresoCaso.Month })
            .Select(g => new { Anio = g.Key.Year, Mes = g.Key.Month, Total = g.Count() })
            .OrderBy(g => g.Anio).ThenBy(g => g.Mes)
            .ToListAsync(ct);
        return datos.Select(x => ($"{x.Anio}-{x.Mes:00}", x.Total)).ToList();
    }

    public async Task<IReadOnlyList<(string Periodo, int Total)>> ObtenerEvolucionTemporalConsolidadosAsync(CancellationToken ct = default)
    {
        var datos = await _db.DelitosNormalizados
            .Where(n => n.VarianteDeId != null)
            .Include(n => n.DelitoOriginal)
            .GroupBy(n => new { n.DelitoOriginal!.FechaIngresoCaso.Year, n.DelitoOriginal.FechaIngresoCaso.Month })
            .Select(g => new { Anio = g.Key.Year, Mes = g.Key.Month, Total = g.Count() })
            .ToListAsync(ct);
        return datos.Select(x => ($"{x.Anio}-{x.Mes:00}", x.Total)).ToList();
    }
}
