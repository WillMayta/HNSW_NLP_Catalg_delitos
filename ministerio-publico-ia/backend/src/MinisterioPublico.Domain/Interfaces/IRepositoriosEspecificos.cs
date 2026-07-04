using MinisterioPublico.Domain.Entities;

namespace MinisterioPublico.Domain.Interfaces;

public interface IDelitoOriginalRepository : IRepositorioGenerico<DelitoOriginal>
{
    Task<IReadOnlyList<DelitoOriginal>> ObtenerPorLoteAsync(Guid loteCargaId, CancellationToken ct = default);
}

public interface IDelitoNormalizadoRepository : IRepositorioGenerico<DelitoNormalizado>
{
    Task<DelitoNormalizado?> ObtenerConEmbeddingAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DelitoNormalizado>> ObtenerSinAgruparAsync(CancellationToken ct = default);
}

public interface IDelitoCatalogoRepository : IRepositorioGenerico<DelitoCatalogo>
{
    Task<IReadOnlyList<DelitoCatalogo>> ObtenerPorFamiliaAsync(Guid familiaId, CancellationToken ct = default);
    Task<DelitoCatalogo?> ObtenerConVariantesAsync(Guid id, CancellationToken ct = default);
}

public interface IPropuestaAgrupamientoRepository : IRepositorioGenerico<PropuestaAgrupamiento>
{
    Task<IReadOnlyList<PropuestaAgrupamiento>> ObtenerPendientesAsync(CancellationToken ct = default);
}

public interface IUsuarioRepository : IRepositorioGenerico<Usuario>
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ObtenerRolesAsync(Guid usuarioId, CancellationToken ct = default);
}

public interface IRegistroAuditoriaRepository : IRepositorioGenerico<RegistroAuditoria>
{
    Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default);
}

/// <summary>
/// Puerto de solo-lectura para consultas analíticas agregadas (Dashboard).
/// Se separa deliberadamente del resto de repositorios transaccionales
/// (aplicación pragmática de CQRS): las lecturas analíticas no necesitan
/// pasar por el modelo de escritura ni por Unit of Work.
/// </summary>
public interface IDashboardQueryRepository
{
    Task<int> ContarDelitosOriginalesAsync(CancellationToken ct = default);
    Task<int> ContarDelitosNormalizadosAsync(CancellationToken ct = default);
    Task<int> ContarDelitosCatalogoConsolidadosAsync(CancellationToken ct = default);
    Task<int> ContarPropuestasPendientesAsync(CancellationToken ct = default);
    Task<int> ContarVariantesAsociadasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(string NombreGenerico, int CantidadVariantes)>> ObtenerDelitosConMayorInconsistenciaAsync(int top, CancellationToken ct = default);
    Task<IReadOnlyList<(string SiglaFiscalia, int CantidadDenominacionesDistintas)>> ObtenerFiscaliasConMayorVariabilidadAsync(int top, CancellationToken ct = default);
    Task<IReadOnlyList<(string Periodo, int Total)>> ObtenerEvolucionTemporalCasosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(string Periodo, int Total)>> ObtenerEvolucionTemporalConsolidadosAsync(CancellationToken ct = default);
}
