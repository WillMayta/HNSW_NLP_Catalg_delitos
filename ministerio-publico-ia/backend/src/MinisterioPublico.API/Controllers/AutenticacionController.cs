using Microsoft.AspNetCore.Mvc;
using MinisterioPublico.Application.Services;

namespace MinisterioPublico.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutenticacionController : ControllerBase
{
    private readonly IAutenticacionService _autenticacionService;

    public AutenticacionController(IAutenticacionService autenticacionService)
        => _autenticacionService = autenticacionService;

    /// <summary>Inicia sesión y devuelve un token JWT válido por 8 horas.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken ct)
    {
        var resultado = await _autenticacionService.AutenticarAsync(request, ct);
        if (resultado is null)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

        return Ok(resultado);
    }
}
