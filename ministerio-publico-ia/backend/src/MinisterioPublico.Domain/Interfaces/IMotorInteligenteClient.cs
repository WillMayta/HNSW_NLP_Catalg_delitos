namespace MinisterioPublico.Domain.Interfaces;

/// <summary>
/// Puerto de salida hacia el microservicio de Inteligencia Artificial
/// (Python: normalización, embeddings, índice HNSW, agrupamiento).
/// La implementación concreta (HttpClient) vive en Infraestructura,
/// manteniendo el dominio agnóstico de cómo se comunica con el motor IA.
/// </summary>
public interface IMotorInteligenteClient
{
    Task<ResultadoNormalizacionDto> NormalizarAsync(string texto, CancellationToken ct = default);
    Task<IReadOnlyList<ResultadoNormalizacionDto>> NormalizarLoteAsync(IEnumerable<string> textos, CancellationToken ct = default);

    Task<float[][]> GenerarEmbeddingsAsync(IEnumerable<string> textos, CancellationToken ct = default);

    Task<int> ConstruirIndiceAsync(IEnumerable<(string Id, string Texto)> registros, CancellationToken ct = default);
    Task InsertarEnIndiceAsync(string id, string texto, CancellationToken ct = default);
    Task ActualizarEnIndiceAsync(string id, string texto, CancellationToken ct = default);
    Task EliminarDeIndiceAsync(string id, CancellationToken ct = default);

    Task<IReadOnlyList<ResultadoBusquedaDto>> BuscarSimilaresAsync(string texto, int k, CancellationToken ct = default);

    Task<IReadOnlyList<PropuestaAgrupamientoDto>> ProponerAgrupamientoAsync(
        IEnumerable<(string Id, string Texto)> registros,
        double umbralSimilitud,
        CancellationToken ct = default);
}

public record ResultadoNormalizacionDto(
    string TextoOriginal,
    string TextoNormalizado,
    string TextoComparable,
    IReadOnlyList<string> ReglasAplicadas);

public record ResultadoBusquedaDto(string IdDelito, double PorcentajeSimilitud);

public record PropuestaAgrupamientoDto(
    string IdPropuesta,
    string DelitoRepresentativoSugerido,
    int CantidadVariantes,
    double CohesionPromedio,
    IReadOnlyList<VarianteAgrupamientoDto> Variantes);

public record VarianteAgrupamientoDto(string Id, string TextoOriginal, string TextoNormalizado);
