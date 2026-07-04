using MinisterioPublico.Application.DTOs;
using MinisterioPublico.Application.Interfaces;
using MinisterioPublico.Domain.Entities;
using MinisterioPublico.Domain.Interfaces;

namespace MinisterioPublico.Application.Services;

/// <summary>
/// Único punto del sistema autorizado a consolidar el Catálogo Penal.
/// Recibe la decisión de un especialista jurídico sobre una
/// PropuestaAgrupamiento y, según el caso:
///   - Aprobar: crea o reutiliza un DelitoCatalogo y asocia las Variantes.
///   - Rechazar: descarta la propuesta, queda como antecedente de auditoría.
///   - AprobarConModificaciones: igual que Aprobar pero con el nombre
///     genérico final corregido por el especialista.
/// Cada validación queda registrada (Aprendizaje Continuo) para
/// retroalimentar futuras propuestas del motor de IA.
/// </summary>
public class ValidacionJuridicaService : IValidacionJuridicaService
{
    private readonly IPropuestaAgrupamientoRepository _propuestas;
    private readonly IDelitoCatalogoRepository _delitosCatalogo;
    private readonly IDelitoNormalizadoRepository _delitosNormalizados;
    private readonly IRepositorioGenerico<Variante> _variantes;
    private readonly IRepositorioGenerico<ValidacionJuridica> _validaciones;
    private readonly IRepositorioGenerico<HistorialCambioCatalogo> _historial;
    private readonly IUnitOfWork _uow;

    public ValidacionJuridicaService(
        IPropuestaAgrupamientoRepository propuestas,
        IDelitoCatalogoRepository delitosCatalogo,
        IDelitoNormalizadoRepository delitosNormalizados,
        IRepositorioGenerico<Variante> variantes,
        IRepositorioGenerico<ValidacionJuridica> validaciones,
        IRepositorioGenerico<HistorialCambioCatalogo> historial,
        IUnitOfWork uow)
    {
        _propuestas = propuestas;
        _delitosCatalogo = delitosCatalogo;
        _delitosNormalizados = delitosNormalizados;
        _variantes = variantes;
        _validaciones = validaciones;
        _historial = historial;
        _uow = uow;
    }

    public async Task<ResultadoValidacionDto> ValidarPropuestaAsync(
        ValidarPropuestaRequestDto request, Guid usuarioValidadorId, CancellationToken ct = default)
    {
        var propuesta = await _propuestas.ObtenerPorIdAsync(request.PropuestaId, ct)
            ?? throw new InvalidOperationException("Propuesta de agrupamiento no encontrada.");

        if (propuesta.Estado != EstadoPropuesta.PendienteValidacion)
            throw new InvalidOperationException("Esta propuesta ya fue validada previamente.");

        var decision = Enum.Parse<DecisionValidacion>(request.Decision, ignoreCase: true);

        if (decision == DecisionValidacion.Rechazar)
        {
            propuesta.Estado = EstadoPropuesta.Rechazada;
            await _propuestas.ActualizarAsync(propuesta, ct);

            await RegistrarValidacionAsync(propuesta.Id, usuarioValidadorId, decision, request.Observaciones, null, ct);

            return new ResultadoValidacionDto(
                Aprobada: false,
                Mensaje: "Propuesta rechazada correctamente. No se generó ningún delito de catálogo.",
                DelitoCatalogoCreado: null);
        }

        if (request.FamiliaDelictivaId is null)
            throw new InvalidOperationException("Debe especificar la familia delictiva para aprobar la propuesta.");

        var nombreGenericoFinal = decision == DecisionValidacion.AprobarConModificaciones
            ? (request.NombreGenericoFinal ?? propuesta.DelitoRepresentativoSugerido)
            : propuesta.DelitoRepresentativoSugerido;

        var delitoCatalogo = new DelitoCatalogo
        {
            NombreGenerico = nombreGenericoFinal,
            FamiliaDelictivaId = request.FamiliaDelictivaId.Value,
            ArticuloPrincipal = request.ArticuloPrincipal,
            Estado = EstadoCatalogo.Validado,
            CreadoPorUsuarioId = usuarioValidadorId,
            ValidadoPorUsuarioId = usuarioValidadorId,
            FechaConsolidacion = DateTime.UtcNow,
        };
        await _delitosCatalogo.AgregarAsync(delitoCatalogo, ct);

        // Asociar cada DelitoNormalizado incluido en la propuesta como Variante
        var idsIncluidos = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
            propuesta.DelitosNormalizadosIncluidosJson) ?? new List<string>();

        foreach (var idStr in idsIncluidos)
        {
            if (!Guid.TryParse(idStr, out var idNormalizado)) continue;

            var normalizado = await _delitosNormalizados.ObtenerPorIdAsync(idNormalizado, ct);
            if (normalizado is null) continue;

            normalizado.VarianteDeId = delitoCatalogo.Id;
            await _delitosNormalizados.ActualizarAsync(normalizado, ct);

            var variante = new Variante
            {
                DelitoCatalogoId = delitoCatalogo.Id,
                DelitoNormalizadoId = normalizado.Id,
                PorcentajeSimilitudOrigen = propuesta.CohesionPromedio,
                AsociadoPorUsuarioId = usuarioValidadorId,
            };
            await _variantes.AgregarAsync(variante, ct);
        }

        propuesta.Estado = decision == DecisionValidacion.AprobarConModificaciones
            ? EstadoPropuesta.AprobadaParcialmente
            : EstadoPropuesta.Aprobada;
        await _propuestas.ActualizarAsync(propuesta, ct);

        await RegistrarValidacionAsync(propuesta.Id, usuarioValidadorId, decision, request.Observaciones, delitoCatalogo.Id, ct);

        await _historial.AgregarAsync(new HistorialCambioCatalogo
        {
            DelitoCatalogoId = delitoCatalogo.Id,
            TipoCambio = "CONSOLIDACION",
            ValorNuevo = nombreGenericoFinal,
            UsuarioId = usuarioValidadorId,
        }, ct);

        var dto = new DelitoCatalogoDto(
            delitoCatalogo.Id, delitoCatalogo.NombreGenerico, delitoCatalogo.Descripcion,
            "", delitoCatalogo.ArticuloPrincipal, delitoCatalogo.Estado.ToString(),
            idsIncluidos.Count, delitoCatalogo.FechaCreacion);

        return new ResultadoValidacionDto(
            Aprobada: true,
            Mensaje: "Propuesta validada y consolidada correctamente en el Catálogo Penal.",
            DelitoCatalogoCreado: dto);
    }

    private async Task RegistrarValidacionAsync(
        Guid propuestaId, Guid usuarioId, DecisionValidacion decision,
        string? observaciones, Guid? delitoCatalogoResultanteId, CancellationToken ct)
    {
        await _validaciones.AgregarAsync(new ValidacionJuridica
        {
            PropuestaAgrupamientoId = propuestaId,
            UsuarioValidadorId = usuarioId,
            Decision = decision,
            Observaciones = observaciones,
            DelitoCatalogoResultanteId = delitoCatalogoResultanteId,
        }, ct);
    }
}
