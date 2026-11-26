namespace Qubriq.API.DTOs.LoadOptimization;

public sealed class LoadOptimizationResponse
{
    public string Algorithm { get; set; } = "ExtremePoint3D-Heuristic";

    public bool IsFeasible { get; set; }

    public string Status { get; set; } = string.Empty;

    public VehicleSpaceDto Vehicle { get; set; } = new();

    public int TotalPackages { get; set; }

    public int PlacedPackages { get; set; }

    public int UnplacedPackages { get; set; }

    public decimal TotalCargoWeightKg { get; set; }

    public decimal PlacedCargoWeightKg { get; set; }

    public decimal RemainingPayloadKg { get; set; }

    public decimal VolumeUtilizationPercent { get; set; }

    public decimal PayloadUtilizationPercent { get; set; }

    public List<PackagePlacementDto> Placements { get; set; } = new();

    public List<UnplacedPackageDto> UnplacedItems { get; set; } = new();

    public List<string> Warnings { get; set; } = new();
}

public sealed class VehicleSpaceDto
{
    public decimal LengthCm { get; set; }

    public decimal WidthCm { get; set; }

    public decimal HeightCm { get; set; }

    public decimal MaxPayloadKg { get; set; }

    public decimal UsedVolumeCm3 { get; set; }

    public decimal TotalVolumeCm3 { get; set; }
}

public sealed class PackagePlacementDto
{
    public string PackageId { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int UnitNumber { get; set; }

    public decimal X { get; set; }

    public decimal Y { get; set; }

    public decimal Z { get; set; }

    public decimal LengthCm { get; set; }

    public decimal WidthCm { get; set; }

    public decimal HeightCm { get; set; }

    public decimal WeightKg { get; set; }

    public bool IsFragile { get; set; }

    public string Orientation { get; set; } = string.Empty;
}

public sealed class UnplacedPackageDto
{
    public string PackageId { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int UnitNumber { get; set; }

    public decimal WeightKg { get; set; }

    public string Reason { get; set; } = string.Empty;
}
