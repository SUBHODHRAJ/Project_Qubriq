using System.ComponentModel.DataAnnotations;

namespace Qubriq.API.DTOs.Admin;

public class CreateCustomerAccountRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;
}
