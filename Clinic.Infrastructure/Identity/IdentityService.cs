using Clinic.Application.Common.Interfaces;
using Clinic.Application.Common.Models;
using Clinic.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Clinic.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<UserLoginInfoModel?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : Map(user);
    }

    public async Task<UserLoginInfoModel?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : Map(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null && await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? Array.Empty<string>() : await _userManager.GetRolesAsync(user);
    }

    private static UserLoginInfoModel Map(ApplicationUser user) => new()
    {
        UserId = user.Id,
        DisplayName = user.UserName ?? user.Email ?? user.Id.ToString(),
        AccountStatus = user.Status.ToString(),
        IsActive = user.Status == AccountStatus.Active
    };
}
