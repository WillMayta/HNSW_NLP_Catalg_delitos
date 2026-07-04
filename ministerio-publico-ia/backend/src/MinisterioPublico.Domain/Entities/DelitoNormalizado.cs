namespace MinisterioPublico.Domain.Entities;

/// <summary>
/// Resultado de aplicar el Motor de Normalización Léxica sobre un
/// DelitoOriginal. Conserva trazabilidad completa de qué reglas se
/// aplicaron, para auditoría y para poder reprocesar si las reglas cambian.
/// </summary>
public class DelitoNormalizado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DelitoOriginalId { get; set; }
    public DelitoOriginal? DelitoOriginal { get; set; }

    public string TextoNormalizado { get; set; } = string.Empty;
    public string TextoComparable { get; set; } = string.Empty; // sin tildes, para indexado

    /// <summary>Reglas de normalización aplicadas, serializadas como JSON (ej. ["mayusculas","eliminacion_articulos_ley"]).</summary>
    public string ReglasAplicadasJson { get; set; } = "[]";

    public DateTime FechaNormalizacion { get; set; } = DateTime.UtcNow;
    public string VersionMotorNormalizacion { get; set; } = "1.0.0";

    // Navegación
    public Embedding? Embedding { get; set; }
    public Guid? VarianteDeId { get; set; } // si fue marcado como variante de un DelitoCatalogo
}
