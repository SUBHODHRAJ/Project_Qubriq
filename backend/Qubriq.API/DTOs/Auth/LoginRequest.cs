using System.ComponentModel.DataAnnotations;

namespace Qubriq.API.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
