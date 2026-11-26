using System.ComponentModel.DataAnnotations;
namespace Qubriq.API.DTOs.Auth;
public sealed class PublicRegistrationRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = "";
    [Required] public string AccountType { get; set; } = "";
    [StringLength(200)] public string? CompanyName { get; set; }
    [StringLength(150)] public string? ContactName { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
}
