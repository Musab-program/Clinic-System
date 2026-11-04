namespace Clinic.Domain.Entities.Authentication;

using Clinic.Domain.Common;


public class RefreshToken : BaseEntity
{

    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public string JwtId { get; set; } = string.Empty;

    public bool IsUsed { get; set; }


    public bool IsRevoked { get; set; }


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiryDate { get; set; }
}
