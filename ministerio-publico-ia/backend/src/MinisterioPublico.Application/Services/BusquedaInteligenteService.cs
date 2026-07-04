using MinisterioPublico.Application.DTOs;
using MinisterioPublico.Application.Interfaces;
using MinisterioPublico.Domain.Interfaces;

namespace MinisterioPublico.Application.Services;

/// <summary>
/// Implementa el flujo "Búsqueda Vectorial" del pipeline: el fiscal o
/// analista ingresa una denominación de delito y el sistema devuelve los
/// registros más similares (vía HNSW), junto con la familia jurídica y el
/// delito de catálogo ya consolidado si la variante fue previamente
/// validada por un especialista.
/// </summary>
public class BusquedaInteligenteService : IBusquedaInteligenteService
{
    private readonly IMotorInteligenteClient _motorIA;
    private readonly IDelitoNormalizadoRepository _delitosNormalizados;
    private readonly IDelitoCatalogoRepository _delitosCatalogo;

    public BusquedaInteligenteService(
        IMotorInteligenteClient motorIA,
        IDelitoNormalizadoRepository delitosNormalizados,
        IDelitoCatalogoRepository delitosCatalogo)
    {
        _motorIA = motorIA;
        _delitosNormalizados = delitosNormalizados;
        _delitosCatalogo = delitosCatalogo;
    }

    public async Task<BusquedaInteligenteResultadoDto> BuscarAsync(
        BusquedaInteligenteRequestDto request, CancellationToken ct = default)
    {
        var inicio = DateTime.UtcNow;

        var normalizacion = await _motorIA.NormalizarAsync(request.TextoDelito, ct);
        var resultadosKnn = await _motorIA.BuscarSimilaresAsync(
            request.TextoDelito, request.CantidadResultados, ct);

        var delitosRelacionados = new List<DelitoSimilarDto>();
        foreach (var r in resultadosKnn)
        {
            // El Id devuelto por el motor HNSW corresponde al Id del
            // DelitoNormalizado (string del Guid); se resuelve metadata
            // jurídica adicional si ya fue validado en el catálogo.
            string? familiaJuridica = null;
            string? delitoCatalogoAsociado = null;

            if (Guid.TryParse(r.IdDelito, out var idNormalizado))
            {
                var normalizado = await _delitosNormalizados.ObtenerPorIdAsync(idNormalizado, ct);
                if (normalizado?.VarianteDeId is { } idCatalogo)
                {
                    var catalogo = await _delitosCatalogo.ObtenerConVariantesAsync(idCatalogo, ct);
                    if (catalogo is not null)
                    {
                        delitoCatalogoAsociado = catalogo.NombreGenerico;
                        familiaJuridica = catalogo.FamiliaDelictiva?.Nombre;
                    }
                }
            }

            delitosRelacionados.Add(new DelitoSimilarDto(
                r.IdDelito,
                normalizacion.TextoNormalizado,
                r.PorcentajeSimilitud,
                familiaJuridica,
                delitoCatalogoAsociado));
        }

        return new BusquedaInteligenteResultadoDto(
            request.TextoDelito,
            normalizacion.TextoNormalizado,
            delitosRelacionados);
    }
}
