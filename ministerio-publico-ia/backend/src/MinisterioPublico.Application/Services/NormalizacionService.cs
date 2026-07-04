using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using MinisterioPublico.Application.DTOs;
using MinisterioPublico.Application.Interfaces;
using MinisterioPublico.Domain.Entities;
using MinisterioPublico.Domain.Interfaces;
using OfficeOpenXml;

namespace MinisterioPublico.Application.Services;

/// <summary>
/// Orquesta el pipeline: Carga histórica -> Motor de Normalización Léxica
/// -> Generación de Embeddings -> Inserción en el Índice HNSW.
/// La normalización en sí (reglas léxicas) se delega siempre al
/// microservicio Python vía IMotorInteligenteClient, para que exista una
/// única fuente de verdad de las reglas (evita duplicar lógica en dos
/// lenguajes y que diverjan con el tiempo).
/// </summary>
public class NormalizacionService : INormalizacionService
{
    private readonly IDelitoOriginalRepository _delitosOriginales;
    private readonly IDelitoNormalizadoRepository _delitosNormalizados;
    private readonly IRepositorioGenerico<Embedding> _embeddings;
    private readonly IRepositorioGenerico<LoteCarga> _lotesCarga;
    private readonly IMotorInteligenteClient _motorIA;
    private readonly IUnitOfWork _uow;

    public NormalizacionService(
        IDelitoOriginalRepository delitosOriginales,
        IDelitoNormalizadoRepository delitosNormalizados,
        IRepositorioGenerico<Embedding> embeddings,
        IRepositorioGenerico<LoteCarga> lotesCarga,
        IMotorInteligenteClient motorIA,
        IUnitOfWork uow)
    {
        _delitosOriginales = delitosOriginales;
        _delitosNormalizados = delitosNormalizados;
        _embeddings = embeddings;
        _lotesCarga = lotesCarga;
        _motorIA = motorIA;
        _uow = uow;
    }

    public async Task<CargaMasivaResultadoDto> ProcesarCargaMasivaAsync(
        IFormFile archivo, Guid usuarioId, CancellationToken ct = default)
    {
        var lote = new LoteCarga
        {
            NombreArchivo = archivo.FileName,
            CargadoPorUsuarioId = usuarioId,
            Estado = EstadoLoteCarga.EnProceso,
        };
        await _lotesCarga.AgregarAsync(lote, ct);

        var filas = LeerArchivo(archivo);
        var errores = new List<string>();
        var originalesCreados = new List<DelitoOriginal>();

        int numeroFila = 1;
        foreach (var fila in filas)
        {
            numeroFila++;
            try
            {
                var original = new DelitoOriginal
                {
                    IdUnicoCaso = fila.GetValueOrDefault("id_unico", ""),
                    Especialidad = fila.GetValueOrDefault("de_esp", ""),
                    TipoDescripcion = fila.GetValueOrDefault("tx_descripcion", ""),
                    TipoCaso = fila.GetValueOrDefault("tx_tipo_caso", ""),
                    FechaIngresoCaso = ParsearFecha(fila.GetValueOrDefault("fe_ing_caso", "")),
                    TextoOriginal = fila.GetValueOrDefault("de_mat_deli", ""),
                    Folio = int.TryParse(fila.GetValueOrDefault("ca_foli", ""), out var folio) ? folio : null,
                    Estado = fila.GetValueOrDefault("de_estado", ""),
                    Acumulado = fila.GetValueOrDefault("st_acumulado", ""),
                    Situacion = fila.GetValueOrDefault("situacion", ""),
                    SiglaFiscalia = fila.GetValueOrDefault("de_sigl_mpub", ""),
                    NombreFiscal = fila.GetValueOrDefault("fiscal", ""),
                    LoteCargaId = lote.Id,
                };

                if (string.IsNullOrWhiteSpace(original.TextoOriginal))
                {
                    errores.Add($"Fila {numeroFila}: el campo de_mat_deli (denominación del delito) está vacío.");
                    continue;
                }

                originalesCreados.Add(original);
            }
            catch (Exception ex)
            {
                errores.Add($"Fila {numeroFila}: {ex.Message}");
            }
        }

        foreach (var original in originalesCreados)
            await _delitosOriginales.AgregarAsync(original, ct);

        // --- Motor de Normalización Léxica (lote, vía microservicio Python) ---
        var textosOriginales = originalesCreados.Select(o => o.TextoOriginal).ToList();
        if (textosOriginales.Count > 0)
        {
            var resultadosNormalizacion = await _motorIA.NormalizarLoteAsync(textosOriginales, ct);

            var normalizados = new List<DelitoNormalizado>();
            for (int i = 0; i < originalesCreados.Count; i++)
            {
                var r = resultadosNormalizacion[i];
                var normalizado = new DelitoNormalizado
                {
                    DelitoOriginalId = originalesCreados[i].Id,
                    TextoNormalizado = r.TextoNormalizado,
                    TextoComparable = r.TextoComparable,
                    ReglasAplicadasJson = System.Text.Json.JsonSerializer.Serialize(r.ReglasAplicadas),
                };
                normalizados.Add(normalizado);
                await _delitosNormalizados.AgregarAsync(normalizado, ct);
            }

            // --- Generación de Embeddings (lote) ---
            var textosNormalizados = normalizados.Select(n => n.TextoNormalizado).ToList();
            var vectores = await _motorIA.GenerarEmbeddingsAsync(textosNormalizados, ct);

            for (int i = 0; i < normalizados.Count; i++)
            {
                var embedding = new Embedding
                {
                    DelitoNormalizadoId = normalizados[i].Id,
                    Vector = vectores[i],
                    Dimension = vectores[i].Length,
                };
                await _embeddings.AgregarAsync(embedding, ct);

                // Construcción/Inserción en el Índice HNSW
                var todosNormalizados = await _delitosNormalizados.ObtenerTodosAsync(ct);
                var registrosParaIndice = todosNormalizados
                    .Select(n => (Id: n.Id.ToString(), Texto: n.TextoNormalizado));
                await _motorIA.ConstruirIndiceAsync(registrosParaIndice, ct);
            }
        }

        lote.CantidadRegistros = originalesCreados.Count;
        lote.CantidadErrores = errores.Count;
        lote.Estado = errores.Count == 0 ? EstadoLoteCarga.Completado : EstadoLoteCarga.CompletadoConErrores;
        lote.FechaFinalizacion = DateTime.UtcNow;
        await _lotesCarga.ActualizarAsync(lote, ct);

        return new CargaMasivaResultadoDto(
            lote.Id, numeroFila - 1, originalesCreados.Count, errores.Count, errores);
    }

    public async Task<DelitoNormalizadoDto> NormalizarYGuardarAsync(Guid delitoOriginalId, CancellationToken ct = default)
    {
        var original = await _delitosOriginales.ObtenerPorIdAsync(delitoOriginalId, ct)
            ?? throw new InvalidOperationException("Delito original no encontrado.");

        var resultado = await _motorIA.NormalizarAsync(original.TextoOriginal, ct);

        var normalizado = new DelitoNormalizado
        {
            DelitoOriginalId = original.Id,
            TextoNormalizado = resultado.TextoNormalizado,
            TextoComparable = resultado.TextoComparable,
            ReglasAplicadasJson = System.Text.Json.JsonSerializer.Serialize(resultado.ReglasAplicadas),
        };
        await _delitosNormalizados.AgregarAsync(normalizado, ct);
        

        var vectores = await _motorIA.GenerarEmbeddingsAsync(new[] { resultado.TextoNormalizado }, ct);
        var embedding = new Embedding
        {
            DelitoNormalizadoId = normalizado.Id,
            Vector = vectores[0],
            Dimension = vectores[0].Length,
        };
        await _embeddings.AgregarAsync(embedding, ct);
        await _motorIA.InsertarEnIndiceAsync(normalizado.Id.ToString(), resultado.TextoNormalizado, ct);

        return new DelitoNormalizadoDto(
            normalizado.Id, original.Id, original.TextoOriginal,
            normalizado.TextoNormalizado, resultado.ReglasAplicadas);
    }

    // ---------- Lectura de archivos (Excel / CSV) ----------

    private static List<Dictionary<string, string>> LeerArchivo(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        using var stream = archivo.OpenReadStream();

        return extension switch
        {
            ".csv" => LeerCsv(stream),
            ".xlsx" or ".xls" => LeerExcel(stream),
            _ => throw new InvalidOperationException(
                $"Formato de archivo no soportado: {extension}. Use .csv o .xlsx."),
        };
    }

    private static List<Dictionary<string, string>> LeerCsv(Stream stream)
    {
        var resultado = new List<Dictionary<string, string>>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
        };
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            var fila = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
                fila[header.Trim()] = csv.GetField(header) ?? "";
            resultado.Add(fila);
        }
        return resultado;
    }

    private static List<Dictionary<string, string>> LeerExcel(Stream stream)
    {
        // Requiere licencia no comercial configurada: ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var resultado = new List<Dictionary<string, string>>();
        using var paquete = new ExcelPackage(stream);
        var hoja = paquete.Workbook.Worksheets.First();
        var encabezados = new List<string>();
        for (int col = 1; col <= hoja.Dimension.Columns; col++)
            encabezados.Add(hoja.Cells[1, col].Text.Trim());

        for (int fila = 2; fila <= hoja.Dimension.Rows; fila++)
        {
            var registro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int col = 1; col <= encabezados.Count; col++)
                registro[encabezados[col - 1]] = hoja.Cells[fila, col].Text.Trim();
            resultado.Add(registro);
        }
        return resultado;
    }

    private static DateTime ParsearFecha(string valor)
    {
        if (DateTime.TryParseExact(valor, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return fecha;
        if (DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;
        return DateTime.MinValue;
    }
}

internal static class DictionaryExtensions
{
    public static string GetValueOrDefault(this Dictionary<string, string> dict, string clave, string porDefecto)
        => dict.TryGetValue(clave, out var valor) ? valor : porDefecto;
}
