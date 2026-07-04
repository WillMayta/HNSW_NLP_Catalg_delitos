using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinisterioPublico.Application.Interfaces;

namespace MinisterioPublico.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    /// <summary>
    /// Indicadores institucionales: cantidad de delitos, variantes
    /// detectadas, delitos con mayor inconsistencia, fiscalías con mayor
    /// variabilidad, evolución temporal y porcentaje de consolidación.
    /// </summary>
    [HttpGet("indicadores")]
    public async Task<IActionResult> ObtenerIndicadores(CancellationToken ct)
        => Ok(await _dashboardService.ObtenerIndicadoresAsync(ct));
}
