using System.ComponentModel.DataAnnotations;

namespace Qubriq.API.DTOs;

public class UpdateCustomerRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? ContactName { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }
}
