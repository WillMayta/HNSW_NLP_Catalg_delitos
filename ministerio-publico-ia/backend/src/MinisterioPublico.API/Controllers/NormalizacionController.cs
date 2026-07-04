using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinisterioPublico.Application.Interfaces;

namespace MinisterioPublico.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NormalizacionController : ControllerBase
{
    private readonly INormalizacionService _normalizacionService;

    public NormalizacionController(INormalizacionService normalizacionService)
        => _normalizacionService = normalizacionService;

    /// <summary>
    /// Carga masiva de delitos históricos desde Excel (.xlsx) o CSV.
    /// Ejecuta automáticamente: normalización léxica + generación de
    /// embeddings para cada registro cargado.
    /// </summary>
    [HttpPost("carga-masiva")]
    [Authorize(Roles = "Administrador,Analista")]
    [RequestSizeLimit(100_000_000)] // 100 MB, soporta los ~50k registros históricos
    public async Task<IActionResult> CargaMasiva(IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { mensaje = "Debe adjuntar un archivo Excel (.xlsx) o CSV." });

        var usuarioId = ObtenerUsuarioId();
        var resultado = await _normalizacionService.ProcesarCargaMasivaAsync(archivo, usuarioId, ct);
        return Ok(resultado);
    }

    /// <summary>Normaliza un único registro ya cargado (útil para reprocesar casos puntuales).</summary>
    [HttpPost("{delitoOriginalId:guid}/normalizar")]
    [Authorize(Roles = "Administrador,Analista")]
    public async Task<IActionResult> NormalizarRegistro(Guid delitoOriginalId, CancellationToken ct)
    {
        var resultado = await _normalizacionService.NormalizarYGuardarAsync(delitoOriginalId, ct);
        return Ok(resultado);
    }

    private Guid ObtenerUsuarioId()
    {
        var sub = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
