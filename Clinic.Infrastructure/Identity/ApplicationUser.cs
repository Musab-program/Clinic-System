namespace Clinic.Infrastructure.Identity;

using Clinic.Domain.Enums;
using Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the application user identity account extending ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
  
    /// <summary>
    /// Gets or sets the lifecycle status of the user account.
    /// </summary>
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    /// <summary>
    /// Gets or sets the timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;



  
}
