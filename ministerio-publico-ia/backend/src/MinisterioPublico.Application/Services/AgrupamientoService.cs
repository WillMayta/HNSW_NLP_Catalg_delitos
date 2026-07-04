using MinisterioPublico.Application.DTOs;
using MinisterioPublico.Application.Interfaces;
using MinisterioPublico.Domain.Entities;
using MinisterioPublico.Domain.Interfaces;

namespace MinisterioPublico.Application.Services;

/// <summary>
/// Orquesta el Motor Inteligente de Agrupamiento. Por diseño explícito del
/// sistema (ver documento de arquitectura), este servicio JAMÁS asigna un
/// delito genérico definitivo: únicamente persiste PropuestaAgrupamiento en
/// estado "PendienteValidacion". La consolidación real ocurre solo a través
/// de IValidacionJuridicaService, ejecutada por un usuario con el rol
/// EspecialistaJuridico.
/// </summary>
public class AgrupamientoService : IAgrupamientoService
{
    private readonly IMotorInteligenteClient _motorIA;
    private readonly IDelitoNormalizadoRepository _delitosNormalizados;
    private readonly IPropuestaAgrupamientoRepository _propuestas;

    public AgrupamientoService(
        IMotorInteligenteClient motorIA,
        IDelitoNormalizadoRepository delitosNormalizados,
        IPropuestaAgrupamientoRepository propuestas)
    {
        _motorIA = motorIA;
        _delitosNormalizados = delitosNormalizados;
        _propuestas = propuestas;
    }

    public async Task<IReadOnlyList<PropuestaAgrupamientoResumenDto>> GenerarPropuestasAsync(
        double umbralSimilitud, CancellationToken ct = default)
    {
        var sinAgrupar = await _delitosNormalizados.ObtenerSinAgruparAsync(ct);
        if (sinAgrupar.Count == 0)
            return Array.Empty<PropuestaAgrupamientoResumenDto>();

        var registros = sinAgrupar.Select(n => (Id: n.Id.ToString(), Texto: n.TextoNormalizado));
        var propuestasMotor = await _motorIA.ProponerAgrupamientoAsync(registros, umbralSimilitud, ct);

        var resultado = new List<PropuestaAgrupamientoResumenDto>();
        foreach (var p in propuestasMotor)
        {
            var entidad = new PropuestaAgrupamiento
            {
                DelitoRepresentativoSugerido = p.DelitoRepresentativoSugerido,
                CantidadVariantes = p.CantidadVariantes,
                CohesionPromedio = p.CohesionPromedio,
                DelitosNormalizadosIncluidosJson = System.Text.Json.JsonSerializer.Serialize(
                    p.Variantes.Select(v => v.Id)),
                Estado = EstadoPropuesta.PendienteValidacion,
            };
            await _propuestas.AgregarAsync(entidad, ct);

            resultado.Add(new PropuestaAgrupamientoResumenDto(
                entidad.Id, p.DelitoRepresentativoSugerido, p.CantidadVariantes,
                p.CohesionPromedio, entidad.Estado.ToString(),
                p.Variantes.Take(5).Select(v => v.TextoOriginal).ToList()));
        }

        return resultado;
    }

    public async Task<IReadOnlyList<PropuestaAgrupamientoResumenDto>> ObtenerPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _propuestas.ObtenerPendientesAsync(ct);
        var resultado = new List<PropuestaAgrupamientoResumenDto>();

        foreach (var p in pendientes)
        {
            var idsIncluidos = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                p.DelitosNormalizadosIncluidosJson) ?? new List<string>();

            var ejemplos = new List<string>();
            foreach (var idStr in idsIncluidos.Take(5))
            {
                if (Guid.TryParse(idStr, out var id))
                {
                    var n = await _delitosNormalizados.ObtenerPorIdAsync(id, ct);
                    if (n is not null) ejemplos.Add(n.TextoNormalizado);
                }
            }

            resultado.Add(new PropuestaAgrupamientoResumenDto(
                p.Id, p.DelitoRepresentativoSugerido, p.CantidadVariantes,
                p.CohesionPromedio, p.Estado.ToString(), ejemplos));
        }

        return resultado;
    }
}
