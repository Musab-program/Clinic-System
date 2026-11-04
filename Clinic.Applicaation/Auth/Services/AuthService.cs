using System.Security.Cryptography;
using System.Text;
using Clinic.Application.Common.Interfaces;
using Clinic.Application.DTOs.Auth;
using Clinic.Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;

    public AuthService(IIdentityService identityService, ITokenService tokenService, IApplicationDbContext context)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _identityService.FindUserByEmailAsync(dto.Email, cancellationToken);
        if (user is null || !await _identityService.CheckPasswordAsync(user.UserId, dto.Password, cancellationToken))
            throw new UnauthorizedAccessException("Invalid credentials.");
        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account is inactive.");

        var roles = await _identityService.GetUserRolesAsync(user.UserId, cancellationToken);
        var tokens = _tokenService.GenerateToken(user.UserId, user.DisplayName, user.AccountStatus, roles);
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            Token = HashToken(tokens.RefreshToken),
            ExpiryDate = tokens.RefreshTokenExpiration,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        return ToResponse(tokens);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        var incomingHash = HashToken(dto.RefreshToken);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == incomingHash, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (storedToken.IsRevoked || storedToken.IsUsed || storedToken.ExpiryDate <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token is expired, used, or revoked.");

        var user = await _identityService.FindUserByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive.");

        var roles = await _identityService.GetUserRolesAsync(user.UserId, cancellationToken);
        var tokens = _tokenService.GenerateToken(user.UserId, user.DisplayName, user.AccountStatus, roles);

        storedToken.IsUsed = true;
        storedToken.IsRevoked = true;
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            Token = HashToken(tokens.RefreshToken),
            ExpiryDate = tokens.RefreshTokenExpiration,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return ToResponse(tokens);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == tokenHash, cancellationToken)
            ?? throw new KeyNotFoundException("Refresh token not found.");
        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static AuthResponseDto ToResponse(Clinic.Application.Common.Models.GenerateTokenModel tokens) => new()
    {
        Token = tokens.Token,
        Expiration = tokens.Expiration,
        RefreshToken = tokens.RefreshToken,
        RefreshTokenExpiration = tokens.RefreshTokenExpiration
    };

    private static string HashToken(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
