namespace MinisterioPublico.Domain.Entities;

/// <summary>
/// Propuesta de agrupación generada automáticamente por el Motor de
/// Agrupamiento Inteligente. El sistema NUNCA asigna un delito genérico
/// definitivo por sí mismo: esta entidad representa una sugerencia que
/// debe pasar obligatoriamente por ValidacionJuridica antes de afectar
/// el Catálogo Penal.
/// </summary>
public class PropuestaAgrupamiento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DelitoRepresentativoSugerido { get; set; } = string.Empty;
    public int CantidadVariantes { get; set; }
    public double CohesionPromedio { get; set; }

    /// <summary>IDs de DelitoNormalizado incluidos en la propuesta, serializados como JSON.</summary>
    public string DelitosNormalizadosIncluidosJson { get; set; } = "[]";

    public EstadoPropuesta Estado { get; set; } = EstadoPropuesta.PendienteValidacion;
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    public string VersionMotorAgrupamiento { get; set; } = "1.0.0";

    public ICollection<ValidacionJuridica> Validaciones { get; set; } = new List<ValidacionJuridica>();
}

public enum EstadoPropuesta
{
    PendienteValidacion = 0,
    Aprobada = 1,
    Rechazada = 2,
    AprobadaParcialmente = 3,
}

/// <summary>
/// Registro de la decisión tomada por un especialista jurídico sobre una
/// PropuestaAgrupamiento. Cada validación retroalimenta el sistema
/// (aprendizaje continuo) y puede disparar reconstrucción del índice HNSW.
/// </summary>
public class ValidacionJuridica
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PropuestaAgrupamientoId { get; set; }
    public PropuestaAgrupamiento? PropuestaAgrupamiento { get; set; }

    public Guid UsuarioValidadorId { get; set; }
    public Usuario? UsuarioValidador { get; set; }

    public DecisionValidacion Decision { get; set; }
    public string? Observaciones { get; set; }
    public Guid? DelitoCatalogoResultanteId { get; set; }

    public DateTime FechaValidacion { get; set; } = DateTime.UtcNow;
}

public enum DecisionValidacion
{
    Aprobar = 0,
    Rechazar = 1,
    AprobarConModificaciones = 2,
}
