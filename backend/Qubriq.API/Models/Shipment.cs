namespace Qubriq.API.Models;

public class Shipment
{
    public long Id { get; set; }

    public string TrackingNumber { get; set; } = string.Empty;

    public long CustomerId { get; set; }

    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string Status { get; set; } = "Draft";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ExpectedDeliveryUtc { get; set; }

    // Cargo details
    public string? CargoName { get; set; }

    public string? CargoCategory { get; set; }

    public int? PackageCount { get; set; }

    public decimal? TotalWeightKg { get; set; }

    public decimal? LengthCm { get; set; }

    public decimal? WidthCm { get; set; }

    public decimal? HeightCm { get; set; }

    public bool? IsFragile { get; set; }

    public string? SpecialHandlingRequirements { get; set; }

    // Transport details
    public string? Priority { get; set; }

    public DateTime? PickupDateUtc { get; set; }

    public string? DeliveryTimeWindow { get; set; }

    public string? AdditionalInstructions { get; set; }

    public Customer Customer { get; set; } = null!;

    public ICollection<ShipmentAssignment> Assignments { get; set; }
        = new List<ShipmentAssignment>();

    public ICollection<ShipmentPackage> Packages { get; set; }
        = new List<ShipmentPackage>();

    public ICollection<OptimizationRun> OptimizationRuns { get; set; }
        = new List<OptimizationRun>();
}

