namespace Clinic.Application.Common.Models;

/// <summary>Minimal user profile needed when issuing access and refresh tokens.</summary>
public sealed class UserLoginInfoModel
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string AccountStatus { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
