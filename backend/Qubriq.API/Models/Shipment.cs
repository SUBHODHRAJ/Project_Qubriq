namespace Qubriq.API.Models;

public class Shipment
{
    public long Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryUtc { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<ShipmentAssignment> Assignments { get; set; } = new List<ShipmentAssignment>();
}
