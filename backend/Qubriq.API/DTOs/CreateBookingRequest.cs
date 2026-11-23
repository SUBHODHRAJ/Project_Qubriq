using System.ComponentModel.DataAnnotations;

namespace Qubriq.API.DTOs;

public class CreateBookingRequest
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [Required]
    [StringLength(250)]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string Destination { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CargoName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CargoCategory { get; set; }

    [Range(1, int.MaxValue)]
    public int PackageCount { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999")]
    public decimal TotalWeightKg { get; set; }

    [Range(typeof(decimal), "0.01", "99999999")]
    public decimal? LengthCm { get; set; }

    [Range(typeof(decimal), "0.01", "99999999")]
    public decimal? WidthCm { get; set; }

    [Range(typeof(decimal), "0.01", "99999999")]
    public decimal? HeightCm { get; set; }

    public bool IsFragile { get; set; }

    [StringLength(500)]
    public string? SpecialHandlingRequirements { get; set; }

    [Required]
    [RegularExpression("^(Low|Normal|High|Urgent)$")]
    public string Priority { get; set; } = "Normal";

    public DateTime PickupDateUtc { get; set; }

    public DateTime? ExpectedDeliveryUtc { get; set; }

    [StringLength(100)]
    public string? DeliveryTimeWindow { get; set; }

    [StringLength(1000)]
    public string? AdditionalInstructions { get; set; }

    [RegularExpression("^(Draft|Confirmed)$")]
    public string Status { get; set; } = "Confirmed";
}
