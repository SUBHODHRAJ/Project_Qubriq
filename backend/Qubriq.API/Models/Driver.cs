namespace Qubriq.API.Models;

public class Driver
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;

    public string Status { get; set; } = "Available";

    public User? User { get; set; }

    public ICollection<ShipmentAssignment> Assignments { get; set; }
        = new List<ShipmentAssignment>();
}