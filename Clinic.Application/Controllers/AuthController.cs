using Clinic.Application.Common.Interfaces;
using Clinic.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto, CancellationToken ct) =>
        Ok(await _authService.LoginAsync(dto, ct));

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto dto, CancellationToken ct) =>
        Ok(await _authService.RefreshTokenAsync(dto, ct));

    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RefreshTokenRequestDto dto, CancellationToken ct)
    {
        await _authService.RevokeTokenAsync(dto.RefreshToken, ct);
        return NoContent();
    }
}
