using System.ComponentModel.DataAnnotations;

namespace Qubriq.API.DTOs.LoadOptimization;

public sealed class LoadOptimizationRequest
{
    [Required]
    [Range(typeof(decimal), "1", "100000")]
    public decimal VehicleLengthCm { get; set; }

    [Required]
    [Range(typeof(decimal), "1", "100000")]
    public decimal VehicleWidthCm { get; set; }

    [Required]
    [Range(typeof(decimal), "1", "100000")]
    public decimal VehicleHeightCm { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal MaxPayloadKg { get; set; }

    [Required]
    [MinLength(1)]
    public List<PackageInputDto> Packages { get; set; } = new();
}

public sealed class PackageInputDto
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string PackageId { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.1", "100000")]
    public decimal LengthCm { get; set; }

    [Range(typeof(decimal), "0.1", "100000")]
    public decimal WidthCm { get; set; }

    [Range(typeof(decimal), "0.1", "100000")]
    public decimal HeightCm { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal WeightKg { get; set; }

    [Range(1, 200)]
    public int Quantity { get; set; } = 1;

    public bool IsFragile { get; set; }
}
