using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinisterioPublico.Application.DTOs;
using MinisterioPublico.Application.Interfaces;

namespace MinisterioPublico.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "EspecialistaJuridico,Administrador")]
public class ValidacionJuridicaController : ControllerBase
{
    private readonly IValidacionJuridicaService _validacionService;

    public ValidacionJuridicaController(IValidacionJuridicaService validacionService)
        => _validacionService = validacionService;

    /// <summary>
    /// Único endpoint del sistema que consolida el Catálogo Penal.
    /// Requiere obligatoriamente el rol EspecialistaJuridico o Administrador.
    /// </summary>
    [HttpPost("validar")]
    public async Task<IActionResult> Validar(ValidarPropuestaRequestDto request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        var usuarioId = Guid.TryParse(sub, out var id) ? id : Guid.Empty;

        var resultado = await _validacionService.ValidarPropuestaAsync(request, usuarioId, ct);
        return Ok(resultado);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CatalogoPenalController : ControllerBase
{
    private readonly ICatalogoPenalService _catalogoService;

    public CatalogoPenalController(ICatalogoPenalService catalogoService)
        => _catalogoService = catalogoService;

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos(CancellationToken ct)
        => Ok(await _catalogoService.ObtenerTodosAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid id, CancellationToken ct)
    {
        var resultado = await _catalogoService.ObtenerPorIdAsync(id, ct);
        return resultado is null ? NotFound() : Ok(resultado);
    }

    [HttpPost]
    [Authorize(Roles = "EspecialistaJuridico,Administrador")]
    public async Task<IActionResult> Crear(CrearDelitoCatalogoRequestDto request, CancellationToken ct)
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        var usuarioId = Guid.TryParse(sub, out var id) ? id : Guid.Empty;

        var resultado = await _catalogoService.CrearAsync(request, usuarioId, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
    }
}
