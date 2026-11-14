using HeatSafe.Api.Configuration;
using HeatSafe.Api.DTOs;
using HeatSafe.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HeatSafe.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    IOptions<DemoAuthOptions> demoAuth,
    JwtTokenService tokenService,
    IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>Emite tokens de demonstração. Disponível somente no ambiente Development.</summary>
    [HttpPost("token")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TokenResponse> CreateToken(TokenRequest request)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var user = demoAuth.Value.Users.FirstOrDefault(x =>
            string.Equals(x.Username, request.Username, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Password, request.Password, StringComparison.Ordinal));
        if (user is null) return Unauthorized(new { mensagem = "Credenciais demonstrativas inválidas." });
        var result = tokenService.Create(user.SubjectId, user.Role, user.Username, user.DeviceCode);
        return Ok(new TokenResponse(result.Token, "Bearer", result.ExpiresAtUtc, user.Role));
    }
}
