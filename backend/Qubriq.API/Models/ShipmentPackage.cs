namespace Qubriq.API.Models;

public class ShipmentPackage
{
    public long Id { get; set; }

    public long ShipmentId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? PackageCode { get; set; }

    public string? Category { get; set; }

    public int Quantity { get; set; }

    public decimal LengthCm { get; set; }

    public decimal WidthCm { get; set; }

    public decimal HeightCm { get; set; }

    public decimal WeightKg { get; set; }

    public bool IsFragile { get; set; }

    public string? SpecialHandlingRequirements { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Shipment Shipment { get; set; } = null!;

    public ICollection<PackagePlacement> Placements { get; set; }
        = new List<PackagePlacement>();
}
