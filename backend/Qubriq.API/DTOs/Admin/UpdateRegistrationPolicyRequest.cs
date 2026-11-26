namespace Qubriq.API.DTOs.Admin;
public sealed class UpdateRegistrationPolicyRequest
{
    public bool PublicRegistrationEnabled { get; set; }
    public bool BookingCustomerSignupEnabled { get; set; }
    public bool FinalRecipientSignupEnabled { get; set; }
    public bool RequireAdminApproval { get; set; }
    public bool ActivatePublicAccountsImmediately { get; set; }
    public bool AdminAccountCreationEnabled { get; set; }
}
