using Clinic.Application.Common.Models;

namespace Clinic.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<UserLoginInfoModel?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserLoginInfoModel?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
    Task<IList<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
}
