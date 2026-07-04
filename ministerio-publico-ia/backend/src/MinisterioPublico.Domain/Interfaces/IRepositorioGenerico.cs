namespace MinisterioPublico.Domain.Interfaces;

/// <summary>
/// Contrato genérico de repositorio (Repository Pattern). Las
/// implementaciones concretas viven en la capa de Infraestructura,
/// manteniendo el Dominio libre de dependencias de EF Core / Npgsql.
/// </summary>
public interface IRepositorioGenerico<TEntidad> where TEntidad : class
{
    Task<TEntidad?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntidad>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<TEntidad> AgregarAsync(TEntidad entidad, CancellationToken ct = default);
    Task ActualizarAsync(TEntidad entidad, CancellationToken ct = default);
    Task EliminarAsync(Guid id, CancellationToken ct = default);
    Task<int> ContarAsync(CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken ct = default);
}
