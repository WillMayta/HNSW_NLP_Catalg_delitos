using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MinisterioPublico.Domain.Interfaces;

namespace MinisterioPublico.Infrastructure.ExternalServices;

/// <summary>
/// Cliente HTTP hacia el microservicio Python (FastAPI) que implementa el
/// Motor de Normalización, el Motor NLP (Sentence Transformers) y el
/// Motor Vectorial HNSW. La URL base se configura vía appsettings.json
/// ("MotorInteligente:BaseUrl") e inyección de IHttpClientFactory.
/// </summary>
public class MotorInteligenteHttpClient : IMotorInteligenteClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public MotorInteligenteHttpClient(HttpClient http) => _http = http;

    public async Task<ResultadoNormalizacionDto> NormalizarAsync(string texto, CancellationToken ct = default)
    {
        var lista = await NormalizarLoteAsync(new[] { texto }, ct);
        return lista.First();
    }

    public async Task<IReadOnlyList<ResultadoNormalizacionDto>> NormalizarLoteAsync(
        IEnumerable<string> textos, CancellationToken ct = default)
    {
        var respuesta = await _http.PostAsJsonAsync("/normalizar", new { textos }, ct);
        respuesta.EnsureSuccessStatusCode();
        var body = await respuesta.Content.ReadFromJsonAsync<RespuestaNormalizacion>(JsonOpts, ct);
        return body!.Resultados.Select(r => new ResultadoNormalizacionDto(
            r.TextoOriginal, r.TextoNormalizado, r.TextoComparable, r.ReglasAplicadas)).ToList();
    }

    public async Task<float[][]> GenerarEmbeddingsAsync(IEnumerable<string> textos, CancellationToken ct = default)
    {
        var respuesta = await _http.PostAsJsonAsync("/embeddings/generar", new { textos }, ct);
        respuesta.EnsureSuccessStatusCode();
        var body = await respuesta.Content.ReadFromJsonAsync<RespuestaEmbeddings>(JsonOpts, ct);
        return body!.Embeddings.Select(e => e.ToArray()).ToArray();
    }

    public async Task<int> ConstruirIndiceAsync(
        IEnumerable<(string Id, string Texto)> registros, CancellationToken ct = default)
    {
        var payload = new { registros = registros.Select(r => new { id = r.Id, texto = r.Texto }) };
        var respuesta = await _http.PostAsJsonAsync("/indice/construir", payload, ct);
        respuesta.EnsureSuccessStatusCode();
        var body = await respuesta.Content.ReadFromJsonAsync<RespuestaConstruirIndice>(JsonOpts, ct);
        return body!.RegistrosIndexados;
    }

    public async Task InsertarEnIndiceAsync(string id, string texto, CancellationToken ct = default)
    {
        var respuesta = await _http.PostAsJsonAsync("/indice/insertar", new { id, texto }, ct);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task ActualizarEnIndiceAsync(string id, string texto, CancellationToken ct = default)
    {
        var respuesta = await _http.PutAsJsonAsync("/indice/actualizar", new { id, texto }, ct);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task EliminarDeIndiceAsync(string id, CancellationToken ct = default)
    {
        var respuesta = await _http.DeleteAsync($"/indice/eliminar/{Uri.EscapeDataString(id)}", ct);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ResultadoBusquedaDto>> BuscarSimilaresAsync(
        string texto, int k, CancellationToken ct = default)
    {
        var respuesta = await _http.PostAsJsonAsync("/busqueda", new { texto, k }, ct);
        respuesta.EnsureSuccessStatusCode();
        var body = await respuesta.Content.ReadFromJsonAsync<RespuestaBusqueda>(JsonOpts, ct);
        return body!.Resultados
            .Select(r => new ResultadoBusquedaDto(r.IdDelito, r.PorcentajeSimilitud))
            .ToList();
    }

    public async Task<IReadOnlyList<PropuestaAgrupamientoDto>> ProponerAgrupamientoAsync(
        IEnumerable<(string Id, string Texto)> registros, double umbralSimilitud, CancellationToken ct = default)
    {
        var payload = new
        {
            registros = registros.Select(r => new { id = r.Id, texto = r.Texto }),
            umbral_similitud = umbralSimilitud
        };
        var respuesta = await _http.PostAsJsonAsync("/agrupamiento/proponer", payload, ct);
        respuesta.EnsureSuccessStatusCode();
        var body = await respuesta.Content.ReadFromJsonAsync<RespuestaAgrupamiento>(JsonOpts, ct);
        return body!.Propuestas.Select(p => new PropuestaAgrupamientoDto(
            p.IdPropuesta, p.DelitoRepresentativoSugerido, p.CantidadVariantes, p.CohesionPromedio,
            p.Variantes.Select(v => new VarianteAgrupamientoDto(v.Id, v.TextoOriginal, v.TextoNormalizado)).ToList()
        )).ToList();
    }

    // ---- DTOs internos de deserialización (espejo del contrato FastAPI) ----
    private record RespuestaNormalizacion(List<ItemNormalizacion> Resultados);
    private record ItemNormalizacion(string TextoOriginal, string TextoNormalizado, string TextoComparable, List<string> ReglasAplicadas);
    private record RespuestaEmbeddings(int Dimension, List<List<float>> Embeddings, string MotorUsado);
    private record RespuestaConstruirIndice(string Mensaje, int RegistrosIndexados);
    private record RespuestaBusqueda(string TextoConsultado, string TextoNormalizado, List<ItemBusqueda> Resultados);
    private record ItemBusqueda(string IdDelito, double PorcentajeSimilitud);
    private record RespuestaAgrupamiento(int CantidadPropuestas, List<ItemPropuesta> Propuestas);
    private record ItemPropuesta(string IdPropuesta, string DelitoRepresentativoSugerido, int CantidadVariantes, double CohesionPromedio, List<ItemVariante> Variantes, string Estado);
    private record ItemVariante(string Id, string TextoOriginal, string TextoNormalizado);
}
