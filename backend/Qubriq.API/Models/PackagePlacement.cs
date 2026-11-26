namespace Qubriq.API.Models;

public class PackagePlacement
{
    public long Id { get; set; }

    public long OptimizationRunId { get; set; }

    public long ShipmentPackageId { get; set; }

    // Identifies an individual unit within a package group
    public int UnitNumber { get; set; }

    // Snapshot of package details at optimization time
    public string PackageDescription { get; set; } = string.Empty;

    public decimal LengthCm { get; set; }

    public decimal WidthCm { get; set; }

    public decimal HeightCm { get; set; }

    public decimal WeightKg { get; set; }

    public bool IsFragile { get; set; }

    // Placed or Unplaced
    public string PlacementStatus { get; set; } = "Unplaced";

    // Coordinates are null for unplaced packages
    public decimal? X { get; set; }

    public decimal? Y { get; set; }

    public decimal? Z { get; set; }

    public string? Orientation { get; set; }

    public string? UnplacedReason { get; set; }

    public OptimizationRun OptimizationRun { get; set; } = null!;

    public ShipmentPackage ShipmentPackage { get; set; } = null!;
}
