using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Clinic.Application.Common.Interfaces;

namespace Clinic.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;
    public string? UserId => _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _accessor.HttpContext?.User.FindFirstValue("sub");
    public bool IsInRole(string role) => _accessor.HttpContext?.User.IsInRole(role) == true;
}
