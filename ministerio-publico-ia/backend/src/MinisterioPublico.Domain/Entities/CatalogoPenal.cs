namespace MinisterioPublico.Domain.Entities;

/// <summary>
/// Agrupación jurídica de alto nivel a la que pertenecen varios delitos
/// genéricos relacionados (ej. "Delitos contra la vida, el cuerpo y la salud").
/// </summary>
public class FamiliaDelictiva
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? TituloCodigoPenal { get; set; } // ej. "Título I - Delitos contra la vida"

    public ICollection<DelitoCatalogo> Delitos { get; set; } = new List<DelitoCatalogo>();
}

/// <summary>
/// Delito genérico consolidado del Catálogo Penal. Construido
/// progresivamente por el especialista jurídico a partir de las
/// propuestas de agrupamiento generadas por el motor inteligente.
/// </summary>
public class DelitoCatalogo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NombreGenerico { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public Guid FamiliaDelictivaId { get; set; }
    public FamiliaDelictiva? FamiliaDelictiva { get; set; }

    /// <summary>Artículo principal del Código Penal asociado (ej. "Art. 376").</summary>
    public string? ArticuloPrincipal { get; set; }

    /// <summary>Leyes complementarias asociadas, serializadas como JSON.</summary>
    public string? LeyesComplementariasJson { get; set; }

    public EstadoCatalogo Estado { get; set; } = EstadoCatalogo.Borrador;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaConsolidacion { get; set; }
    public Guid CreadoPorUsuarioId { get; set; }
    public Guid? ValidadoPorUsuarioId { get; set; }

    public ICollection<Variante> Variantes { get; set; } = new List<Variante>();
}

/// <summary>
/// Vincula un DelitoNormalizado (una denominación específica encontrada en
/// los registros históricos) con el DelitoCatalogo genérico al que
/// pertenece, tras la validación jurídica correspondiente.
/// </summary>
public class Variante
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DelitoCatalogoId { get; set; }
    public DelitoCatalogo? DelitoCatalogo { get; set; }

    public Guid DelitoNormalizadoId { get; set; }
    public DelitoNormalizado? DelitoNormalizado { get; set; }

    public double PorcentajeSimilitudOrigen { get; set; }
    public DateTime FechaAsociacion { get; set; } = DateTime.UtcNow;
    public Guid AsociadoPorUsuarioId { get; set; }
    public string? Observaciones { get; set; }
}

public enum EstadoCatalogo
{
    Borrador = 0,
    PendienteValidacion = 1,
    Validado = 2,
    Consolidado = 3,
    Rechazado = 4,
}
