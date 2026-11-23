namespace Qubriq.API.Models;

public class Vehicle
{
    public long Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public decimal CapacityKg { get; set; }
    public string Status { get; set; } = "Available";

    public ICollection<ShipmentAssignment> Assignments { get; set; } = new List<ShipmentAssignment>();
}
