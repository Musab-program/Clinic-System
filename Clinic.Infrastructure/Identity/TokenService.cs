namespace Clinic.Infrastructure.Identity;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Clinic.Application.Common.Interfaces;
using Clinic.Application.Common.Models;


public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

     public GenerateTokenModel GenerateToken(
        Guid userId,
        string firstName,
        string accountStatus,
        IEnumerable<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiryMinutes = double.Parse(jwtSettings["ExpiryMinutes"] ?? "15");
        var refreshExpiryHours = double.Parse(jwtSettings["RefreshTokenExpiryHours"] ?? "3");

        var expire = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var refreshExpire = DateTime.UtcNow.AddHours(refreshExpiryHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Name, firstName),
            new("accountStatus", accountStatus),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

      

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expire,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        // Generate cryptographically secure random refresh token bytes
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshTokenString = Convert.ToBase64String(randomBytes);

        return new GenerateTokenModel
        {
            Token = tokenString,
            Expiration = expire,
            RefreshToken = refreshTokenString,
            RefreshTokenExpiration = refreshExpire
        };
    }
}
