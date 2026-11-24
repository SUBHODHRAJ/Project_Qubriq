namespace Qubriq.API.Models;

public class ShipmentRecipient
{
    public long Id { get; set; }

    public long ShipmentId { get; set; }

    public long UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Shipment Shipment { get; set; } = null!;

    public User User { get; set; } = null!;
}