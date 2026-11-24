namespace Qubriq.API.Models;

public class DeliveryProof
{
    public long Id { get; set; }

    public long ShipmentId { get; set; }

    public long SubmittedByUserId { get; set; }

    public string ProofType { get; set; } = string.Empty;

    public string? FileReference { get; set; }

    public string? Remarks { get; set; }

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    public Shipment Shipment { get; set; } = null!;

    public User SubmittedByUser { get; set; } = null!;
}