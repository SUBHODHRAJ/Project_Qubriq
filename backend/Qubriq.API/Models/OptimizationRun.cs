namespace Qubriq.API.Models;

public class OptimizationRun
{
    public long Id { get; set; }

    public long ShipmentId { get; set; }

    public long VehicleId { get; set; }

    public long CreatedByUserId { get; set; }

    public string Algorithm { get; set; } = "ExtremePoint3D-Heuristic";

    public string Status { get; set; } = "Completed";

    public bool IsFeasible { get; set; }

    // Vehicle dimensions and capacity at optimization time
    public decimal VehicleLengthCm { get; set; }

    public decimal VehicleWidthCm { get; set; }

    public decimal VehicleHeightCm { get; set; }

    public decimal VehicleCapacityKg { get; set; }

    public int TotalPackages { get; set; }

    public int PlacedPackages { get; set; }

    public int UnplacedPackages { get; set; }

    public decimal TotalWeightKg { get; set; }

    public decimal UsedWeightKg { get; set; }

    public decimal WeightUtilizationPercent { get; set; }

    public decimal VolumeUtilizationPercent { get; set; }

    public string? Warnings { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Shipment Shipment { get; set; } = null!;

    public Vehicle Vehicle { get; set; } = null!;

    public User CreatedByUser { get; set; } = null!;

    public ICollection<PackagePlacement> Placements { get; set; }
        = new List<PackagePlacement>();

    public ICollection<LoadingPlanApproval> Approvals { get; set; }
        = new List<LoadingPlanApproval>();
}
