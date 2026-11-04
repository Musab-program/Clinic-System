using System;

namespace Clinic.Application.Common.Models;

/// <summary>
/// Encapsulates authentication tokens generated for an authenticated session.
/// </summary>
public class GenerateTokenModel
{
    /// <summary>
    /// Gets or sets the encoded JSON Web Token (JWT) access string.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration date and time of the access token.
    /// </summary>
    public DateTime Expiration { get; set; }

    /// <summary>
    /// Gets or sets the cryptographically random refresh token string.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration date and time of the refresh token.
    /// </summary>
    public DateTime RefreshTokenExpiration { get; set; }
}
