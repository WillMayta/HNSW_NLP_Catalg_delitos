using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinisterioPublico.Application.Interfaces;

namespace MinisterioPublico.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgrupamientoController : ControllerBase
{
    private readonly IAgrupamientoService _agrupamientoService;

    public AgrupamientoController(IAgrupamientoService agrupamientoService)
        => _agrupamientoService = agrupamientoService;

    /// <summary>
    /// Ejecuta el Motor Inteligente sobre todos los delitos normalizados
    /// que aún no han sido agrupados, generando PROPUESTAS de agrupación
    /// (nunca asigna un delito genérico definitivo de forma automática).
    /// </summary>
    [HttpPost("generar-propuestas")]
    [Authorize(Roles = "Administrador,Analista")]
    public async Task<IActionResult> GenerarPropuestas([FromQuery] double umbralSimilitud = 80.0, CancellationToken ct = default)
    {
        var propuestas = await _agrupamientoService.GenerarPropuestasAsync(umbralSimilitud, ct);
        return Ok(propuestas);
    }

    /// <summary>Lista las propuestas de agrupamiento pendientes de validación jurídica.</summary>
    [HttpGet("pendientes")]
    public async Task<IActionResult> ObtenerPendientes(CancellationToken ct)
    {
        var pendientes = await _agrupamientoService.ObtenerPendientesAsync(ct);
        return Ok(pendientes);
    }
}
