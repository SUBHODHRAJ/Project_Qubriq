namespace Qubriq.API.Models;

public class Vehicle
{
    public long Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public decimal CapacityKg { get; set; }

    // Internal cargo compartment dimensions in centimetres.
    // Nullable for compatibility with existing vehicle records.
    public decimal? CargoLengthCm { get; set; }
    public decimal? CargoWidthCm { get; set; }
    public decimal? CargoHeightCm { get; set; }

    public string Status { get; set; } = "Available";

    public ICollection<ShipmentAssignment> Assignments { get; set; }
        = new List<ShipmentAssignment>();

    public ICollection<OptimizationRun> OptimizationRuns { get; set; }
        = new List<OptimizationRun>();
}

