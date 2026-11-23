namespace Qubriq.API.Models;

public class ShipmentAssignment
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public long VehicleId { get; set; }
    public long DriverId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAtUtc { get; set; }
    public string Status { get; set; } = "Active";

    public Shipment Shipment { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Driver Driver { get; set; } = null!;
}
