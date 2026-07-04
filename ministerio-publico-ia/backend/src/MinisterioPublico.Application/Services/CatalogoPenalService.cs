using MinisterioPublico.Application.DTOs;
using MinisterioPublico.Application.Interfaces;
using MinisterioPublico.Domain.Entities;
using MinisterioPublico.Domain.Interfaces;

namespace MinisterioPublico.Application.Services;

public class CatalogoPenalService : ICatalogoPenalService
{
    private readonly IDelitoCatalogoRepository _delitosCatalogo;
    private readonly IRepositorioGenerico<FamiliaDelictiva> _familias;

    public CatalogoPenalService(
        IDelitoCatalogoRepository delitosCatalogo,
        IRepositorioGenerico<FamiliaDelictiva> familias)
    {
        _delitosCatalogo = delitosCatalogo;
        _familias = familias;
    }

    public async Task<DelitoCatalogoDto> CrearAsync(
        CrearDelitoCatalogoRequestDto request, Guid usuarioId, CancellationToken ct = default)
    {
        var familia = await _familias.ObtenerPorIdAsync(request.FamiliaDelictivaId, ct)
            ?? throw new InvalidOperationException("La familia delictiva especificada no existe.");

        var entidad = new DelitoCatalogo
        {
            NombreGenerico = request.NombreGenerico,
            Descripcion = request.Descripcion,
            FamiliaDelictivaId = request.FamiliaDelictivaId,
            ArticuloPrincipal = request.ArticuloPrincipal,
            LeyesComplementariasJson = request.LeyesComplementarias is { Count: > 0 }
                ? System.Text.Json.JsonSerializer.Serialize(request.LeyesComplementarias)
                : null,
            Estado = EstadoCatalogo.Borrador,
            CreadoPorUsuarioId = usuarioId,
        };
        await _delitosCatalogo.AgregarAsync(entidad, ct);

        return new DelitoCatalogoDto(
            entidad.Id, entidad.NombreGenerico, entidad.Descripcion, familia.Nombre,
            entidad.ArticuloPrincipal, entidad.Estado.ToString(), 0, entidad.FechaCreacion);
    }

    public async Task<IReadOnlyList<DelitoCatalogoDto>> ObtenerTodosAsync(CancellationToken ct = default)
    {
        var todos = await _delitosCatalogo.ObtenerTodosAsync(ct);
        var resultado = new List<DelitoCatalogoDto>();
        foreach (var d in todos)
        {
            var conVariantes = await _delitosCatalogo.ObtenerConVariantesAsync(d.Id, ct);
            resultado.Add(new DelitoCatalogoDto(
                d.Id, d.NombreGenerico, d.Descripcion,
                conVariantes?.FamiliaDelictiva?.Nombre ?? "",
                d.ArticuloPrincipal, d.Estado.ToString(),
                conVariantes?.Variantes.Count ?? 0, d.FechaCreacion));
        }
        return resultado;
    }

    public async Task<DelitoCatalogoDto?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
    {
        var d = await _delitosCatalogo.ObtenerConVariantesAsync(id, ct);
        if (d is null) return null;

        return new DelitoCatalogoDto(
            d.Id, d.NombreGenerico, d.Descripcion, d.FamiliaDelictiva?.Nombre ?? "",
            d.ArticuloPrincipal, d.Estado.ToString(), d.Variantes.Count, d.FechaCreacion);
    }
}
