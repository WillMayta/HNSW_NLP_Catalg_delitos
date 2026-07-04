using Microsoft.AspNetCore.Http;
using MinisterioPublico.Application.DTOs;

namespace MinisterioPublico.Application.Interfaces;

public interface INormalizacionService
{
    Task<CargaMasivaResultadoDto> ProcesarCargaMasivaAsync(IFormFile archivo, Guid usuarioId, CancellationToken ct = default);
    Task<DelitoNormalizadoDto> NormalizarYGuardarAsync(Guid delitoOriginalId, CancellationToken ct = default);
}

public interface IBusquedaInteligenteService
{
    Task<BusquedaInteligenteResultadoDto> BuscarAsync(BusquedaInteligenteRequestDto request, CancellationToken ct = default);
}

public interface IAgrupamientoService
{
    Task<IReadOnlyList<PropuestaAgrupamientoResumenDto>> GenerarPropuestasAsync(double umbralSimilitud, CancellationToken ct = default);
    Task<IReadOnlyList<PropuestaAgrupamientoResumenDto>> ObtenerPendientesAsync(CancellationToken ct = default);
}

public interface IValidacionJuridicaService
{
    Task<ResultadoValidacionDto> ValidarPropuestaAsync(ValidarPropuestaRequestDto request, Guid usuarioValidadorId, CancellationToken ct = default);
}

public interface ICatalogoPenalService
{
    Task<DelitoCatalogoDto> CrearAsync(CrearDelitoCatalogoRequestDto request, Guid usuarioId, CancellationToken ct = default);
    Task<IReadOnlyList<DelitoCatalogoDto>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<DelitoCatalogoDto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<IndicadoresDashboardDto> ObtenerIndicadoresAsync(CancellationToken ct = default);
}
