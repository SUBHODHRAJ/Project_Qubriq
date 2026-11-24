using System.ComponentModel.DataAnnotations;

namespace Qubriq.API.DTOs.Auth;

public class BootstrapAdminRequest
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(12)]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
