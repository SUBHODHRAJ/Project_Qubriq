namespace Qubriq.API.Models;
public class AccountRegistrationPolicy
{
    public int Id { get; set; } = 1;
    public bool PublicRegistrationEnabled { get; set; } = true;
    public bool BookingCustomerSignupEnabled { get; set; } = true;
    public bool FinalRecipientSignupEnabled { get; set; } = false;
    public bool RequireAdminApproval { get; set; } = false;
    public bool ActivatePublicAccountsImmediately { get; set; } = true;
    public bool AdminAccountCreationEnabled { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public long? UpdatedByUserId { get; set; }
}
