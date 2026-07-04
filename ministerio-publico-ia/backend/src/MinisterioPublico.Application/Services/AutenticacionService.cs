using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MinisterioPublico.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MinisterioPublico.Application.Services;

public record LoginRequestDto(string NombreUsuario, string Contrasena);
public record LoginResultadoDto(string Token, string NombreCompleto, IReadOnlyList<string> Roles, DateTime ExpiraEn);

public interface IAutenticacionService
{
    Task<LoginResultadoDto?> AutenticarAsync(LoginRequestDto request, CancellationToken ct = default);
}

/// <summary>
/// Autenticación basada en JWT. La contraseña se valida con BCrypt
/// (hash + salt). El token incluye los roles del usuario como claims para
/// que [Authorize(Roles = "...")] funcione directamente en los controladores.
/// </summary>
public class AutenticacionService : IAutenticacionService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly string _claveSecreta;
    private readonly string _emisor;
    private readonly string _audiencia;

    public AutenticacionService(IUsuarioRepository usuarios, IConfiguration config)
    {
        _usuarios = usuarios;
        _claveSecreta = config["Jwt:ClaveSecreta"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:ClaveSecreta en appsettings.json");
        _emisor = config["Jwt:Emisor"] ?? "MinisterioPublicoIA";
        _audiencia = config["Jwt:Audiencia"] ?? "MinisterioPublicoIA.Clientes";
    }

    public async Task<LoginResultadoDto?> AutenticarAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorNombreUsuarioAsync(request.NombreUsuario, ct);
        if (usuario is null || !usuario.Activo)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Contrasena, usuario.HashContrasena))
            return null;

        var roles = await _usuarios.ObtenerRolesAsync(usuario.Id, ct);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Email, usuario.Correo),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expiracion = DateTime.UtcNow.AddHours(8);
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_claveSecreta)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _emisor,
            audience: _audiencia,
            claims: claims,
            expires: expiracion,
            signingCredentials: credenciales);

        return new LoginResultadoDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            usuario.NombreCompleto,
            roles,
            expiracion);
    }
}
