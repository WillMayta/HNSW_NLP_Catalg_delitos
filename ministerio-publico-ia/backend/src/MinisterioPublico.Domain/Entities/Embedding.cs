namespace MinisterioPublico.Domain.Entities;

/// <summary>
/// Vector de embedding generado por el motor NLP (Sentence Transformers,
/// modelo all-MiniLM-L6-v2) para un DelitoNormalizado. El vector en sí se
/// almacena serializado (float[] -> bytes) porque PostgreSQL + pgvector
/// maneja el tipo `vector` nativamente vía Npgsql; aquí se modela como
/// arreglo de floats para que el ORM (EF Core) pueda mapearlo con el
/// proveedor `Npgsql.EntityFrameworkCore.PostgreSQL` + pgvector.
/// </summary>
public class Embedding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DelitoNormalizadoId { get; set; }
    public DelitoNormalizado? DelitoNormalizado { get; set; }

    /// <summary>Vector de 384 dimensiones (all-MiniLM-L6-v2). Mapeado a tipo `vector(384)` en PostgreSQL/pgvector.</summary>
    public float[] Vector { get; set; } = Array.Empty<float>();

    public int Dimension { get; set; } = 384;
    public string ModeloUtilizado { get; set; } = "all-MiniLM-L6-v2";
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;

    /// <summary>Posición/etiqueta del vector dentro del índice HNSW vigente, para soportar actualización puntual.</summary>
    public long? IdEnIndiceHnsw { get; set; }
}
