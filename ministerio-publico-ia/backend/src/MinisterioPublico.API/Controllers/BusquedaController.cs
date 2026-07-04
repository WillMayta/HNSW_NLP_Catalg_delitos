using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinisterioPublico.Application.DTOs;
using MinisterioPublico.Application.Interfaces;

namespace MinisterioPublico.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BusquedaController : ControllerBase
{
    private readonly IBusquedaInteligenteService _busquedaService;

    public BusquedaController(IBusquedaInteligenteService busquedaService)
        => _busquedaService = busquedaService;

    /// <summary>
    /// Búsqueda inteligente: ingresa una denominación de delito y devuelve
    /// los registros más similares (vía índice HNSW), con porcentaje de
    /// similitud, familia jurídica y delito de catálogo asociado si ya
    /// fue validado previamente.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Buscar(BusquedaInteligenteRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TextoDelito))
            return BadRequest(new { mensaje = "Debe ingresar el texto del delito a buscar." });

        var resultado = await _busquedaService.BuscarAsync(request, ct);
        return Ok(resultado);
    }
}
