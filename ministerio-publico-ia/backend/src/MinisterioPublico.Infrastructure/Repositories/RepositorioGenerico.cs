using Microsoft.EntityFrameworkCore;
using MinisterioPublico.Domain.Interfaces;
using MinisterioPublico.Infrastructure.Persistence;

namespace MinisterioPublico.Infrastructure.Repositories;

public class RepositorioGenerico<TEntidad> : IRepositorioGenerico<TEntidad>
    where TEntidad : class
{
    protected readonly MinisterioPublicoDbContext _contexto;
    protected readonly DbSet<TEntidad> _conjunto;

    public RepositorioGenerico(MinisterioPublicoDbContext contexto)
    {
        _contexto = contexto;
        _conjunto = contexto.Set<TEntidad>();
    }

    public virtual async Task<TEntidad?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
        => await _conjunto.FindAsync(new object[] { id }, ct);

    public virtual async Task<IReadOnlyList<TEntidad>> ObtenerTodosAsync(CancellationToken ct = default)
        => await _conjunto.ToListAsync(ct);

    public virtual async Task<TEntidad> AgregarAsync(TEntidad entidad, CancellationToken ct = default)
    {
        await _conjunto.AddAsync(entidad, ct);
        await _contexto.SaveChangesAsync(ct);
        return entidad;
    }

    public virtual async Task ActualizarAsync(TEntidad entidad, CancellationToken ct = default)
    {
        _conjunto.Update(entidad);
        await _contexto.SaveChangesAsync(ct);
    }

    public virtual async Task EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var entidad = await ObtenerPorIdAsync(id, ct);
        if (entidad is not null)
        {
            _conjunto.Remove(entidad);
            await _contexto.SaveChangesAsync(ct);
        }
    }

    public virtual async Task<int> ContarAsync(CancellationToken ct = default)
        => await _conjunto.CountAsync(ct);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly MinisterioPublicoDbContext _contexto;
    public UnitOfWork(MinisterioPublicoDbContext contexto) => _contexto = contexto;
    public Task<int> GuardarCambiosAsync(CancellationToken ct = default) => _contexto.SaveChangesAsync(ct);
}
