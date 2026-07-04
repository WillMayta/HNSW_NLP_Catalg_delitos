using Microsoft.EntityFrameworkCore;
using MinisterioPublico.Domain.Entities;
using MinisterioPublico.Domain.Interfaces;
using MinisterioPublico.Infrastructure.Persistence;

namespace MinisterioPublico.Infrastructure.Repositories;

public class DelitoOriginalRepository : RepositorioGenerico<DelitoOriginal>, IDelitoOriginalRepository
{
    public DelitoOriginalRepository(MinisterioPublicoDbContext ctx) : base(ctx) { }

    public async Task<IReadOnlyList<DelitoOriginal>> ObtenerPorLoteAsync(Guid loteCargaId, CancellationToken ct = default)
        => await _conjunto.Where(d => d.LoteCargaId == loteCargaId).ToListAsync(ct);
}

public class DelitoNormalizadoRepository : RepositorioGenerico<DelitoNormalizado>, IDelitoNormalizadoRepository
{
    public DelitoNormalizadoRepository(MinisterioPublicoDbContext ctx) : base(ctx) { }

    public async Task<DelitoNormalizado?> ObtenerConEmbeddingAsync(Guid id, CancellationToken ct = default)
        => await _conjunto
            .Include(n => n.Embedding)
            .Include(n => n.DelitoOriginal)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<DelitoNormalizado>> ObtenerSinAgruparAsync(CancellationToken ct = default)
        => await _conjunto.Where(n => n.VarianteDeId == null).ToListAsync(ct);
}

public class DelitoCatalogoRepository : RepositorioGenerico<DelitoCatalogo>, IDelitoCatalogoRepository
{
    public DelitoCatalogoRepository(MinisterioPublicoDbContext ctx) : base(ctx) { }

    public async Task<IReadOnlyList<DelitoCatalogo>> ObtenerPorFamiliaAsync(Guid familiaId, CancellationToken ct = default)
        => await _conjunto.Where(d => d.FamiliaDelictivaId == familiaId).ToListAsync(ct);

    public async Task<DelitoCatalogo?> ObtenerConVariantesAsync(Guid id, CancellationToken ct = default)
        => await _conjunto
            .Include(d => d.Variantes)
                .ThenInclude(v => v.DelitoNormalizado)
            .Include(d => d.FamiliaDelictiva)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
}

public class PropuestaAgrupamientoRepository : RepositorioGenerico<PropuestaAgrupamiento>, IPropuestaAgrupamientoRepository
{
    public PropuestaAgrupamientoRepository(MinisterioPublicoDbContext ctx) : base(ctx) { }

    public async Task<IReadOnlyList<PropuestaAgrupamiento>> ObtenerPendientesAsync(CancellationToken ct = default)
        => await _conjunto
            .Where(p => p.Estado == EstadoPropuesta.PendienteValidacion)
            .OrderByDescending(p => p.CantidadVariantes)
            .ToListAsync(ct);
}

public class UsuarioRepository : RepositorioGenerico<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(MinisterioPublicoDbContext ctx) : base(ctx) { }

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default)
        => await _conjunto.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, ct);

    public async Task<IReadOnlyList<string>> ObtenerRolesAsync(Guid usuarioId, CancellationToken ct = default)
        => await _contexto.UsuarioRoles
            .Where(ur => ur.UsuarioId == usuarioId)
            .Join(_contexto.Roles, ur => ur.RolId, r => r.Id, (ur, r) => r.Nombre)
            .ToListAsync(ct);
}

public class RegistroAuditoriaRepository : RepositorioGenerico<RegistroAuditoria>, IRegistroAuditoriaRepository
{
    public RegistroAuditoriaRepository(MinisterioPublicoDbContext ctx) : base(ctx) { }

    public async Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default)
    {
        await _conjunto.AddAsync(registro, ct);
        await _contexto.SaveChangesAsync(ct);
    }
}
