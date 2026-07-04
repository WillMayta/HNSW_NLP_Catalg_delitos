namespace MinisterioPublico.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string HashContrasena { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

public class Rol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty; // Administrador, EspecialistaJuridico, Analista, Consulta
    public string? Descripcion { get; set; }

    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}

public class Permiso
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = string.Empty; // ej. "catalogo.validar", "catalogo.crear"
    public string? Descripcion { get; set; }

    public ICollection<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
}

// Tablas intermedias (relación muchos a muchos), normalizadas a 3FN
public class UsuarioRol
{
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public Guid RolId { get; set; }
    public Rol? Rol { get; set; }
}

public class RolPermiso
{
    public Guid RolId { get; set; }
    public Rol? Rol { get; set; }
    public Guid PermisoId { get; set; }
    public Permiso? Permiso { get; set; }
}

/// <summary>
/// Registro de auditoría genérico para trazabilidad de todas las acciones
/// relevantes del sistema (creación, edición, validación, eliminación).
/// </summary>
public class RegistroAuditoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;      // ej. "VALIDAR_PROPUESTA", "CARGA_MASIVA"
    public string EntidadAfectada { get; set; } = string.Empty;
    public string? EntidadId { get; set; }
    public string? DatosAnteriores { get; set; } // JSON
    public string? DatosNuevos { get; set; }     // JSON
    public string DireccionIp { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Parámetro configurable del Motor de Normalización (palabras irrelevantes,
/// abreviaturas, patrones de artículos de ley, etc.) para que el motor sea
/// ajustable sin recompilar el sistema.
/// </summary>
public class ParametroNormalizacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TipoParametro { get; set; } = string.Empty; // "ABREVIATURA", "PALABRA_IRRELEVANTE", "PATRON_ARTICULO"
    public string Clave { get; set; } = string.Empty;
    public string? Valor { get; set; }
    public bool Activo { get; set; } = true;
    public Guid CreadoPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Historial de cambios sobre el Catálogo Penal, para trazabilidad
/// específica de la evolución de cada delito genérico consolidado.
/// </summary>
public class HistorialCambioCatalogo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DelitoCatalogoId { get; set; }
    public DelitoCatalogo? DelitoCatalogo { get; set; }
    public string TipoCambio { get; set; } = string.Empty; // "CREACION", "EDICION", "CONSOLIDACION", "RECHAZO"
    public string? ValorAnterior { get; set; }
    public string? ValorNuevo { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
}

/// <summary>Representa una carga masiva (Excel/CSV) de delitos históricos.</summary>
public class LoteCarga
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NombreArchivo { get; set; } = string.Empty;
    public int CantidadRegistros { get; set; }
    public int CantidadErrores { get; set; }
    public EstadoLoteCarga Estado { get; set; } = EstadoLoteCarga.EnProceso;
    public Guid CargadoPorUsuarioId { get; set; }
    public DateTime FechaCarga { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFinalizacion { get; set; }
}

public enum EstadoLoteCarga
{
    EnProceso = 0,
    Completado = 1,
    CompletadoConErrores = 2,
    Fallido = 3,
}
