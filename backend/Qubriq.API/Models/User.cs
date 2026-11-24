namespace Qubriq.API.Models;

public class User
{
    public long Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Nullable because Google-only accounts do not need a website password.
    public string? PasswordHash { get; set; }

    public bool EmailVerified { get; set; } = false;

    public string? RecipientType { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    public ICollection<ExternalLogin> ExternalLogins { get; set; }
        = new List<ExternalLogin>();

    public Customer? Customer { get; set; }

    public Driver? Driver { get; set; }

    public ICollection<ShipmentRecipient> ShipmentRecipients { get; set; }
        = new List<ShipmentRecipient>();

    public ICollection<DeliveryProof> DeliveryProofs { get; set; }
        = new List<DeliveryProof>();

    public ICollection<AuditLog> AuditLogs { get; set; }
        = new List<AuditLog>();
}