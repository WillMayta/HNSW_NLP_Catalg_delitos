namespace MinisterioPublico.Domain.Entities;

/// <summary>
/// Representa un registro histórico de delito tal como fue ingresado
/// originalmente en el sistema del Ministerio Público, sin ninguna
/// transformación. Esta entidad nunca se modifica tras su carga: el texto
/// original siempre se conserva como evidencia de auditoría y como fuente
/// de verdad legal del expediente.
/// </summary>
public class DelitoOriginal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Identificador único del caso tal como llega del sistema fuente (id_unico).</summary>
    public string IdUnicoCaso { get; set; } = string.Empty;

    public string Especialidad { get; set; } = string.Empty;       // de_esp
    public string TipoDescripcion { get; set; } = string.Empty;    // tx_descripcion
    public string TipoCaso { get; set; } = string.Empty;           // tx_tipo_caso
    public DateTime FechaIngresoCaso { get; set; }                 // fe_ing_caso

    /// <summary>Denominación original del delito tal como fue registrada (de_mat_deli). NUNCA se modifica.</summary>
    public string TextoOriginal { get; set; } = string.Empty;

    public int? Folio { get; set; }                                // ca_foli
    public string Estado { get; set; } = string.Empty;             // de_estado
    public string? Acumulado { get; set; }                         // st_acumulado
    public string Situacion { get; set; } = string.Empty;          // situacion
    public string SiglaFiscalia { get; set; } = string.Empty;      // de_sigl_mpub
    public string NombreFiscal { get; set; } = string.Empty;       // fiscal

    public DateTime FechaCarga { get; set; } = DateTime.UtcNow;
    public Guid LoteCargaId { get; set; }

    // Navegación
    public DelitoNormalizado? DelitoNormalizado { get; set; }
}
