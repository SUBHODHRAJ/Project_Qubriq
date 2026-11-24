namespace Qubriq.API.Models;

public class Customer
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public ICollection<Shipment> Shipments { get; set; }
        = new List<Shipment>();
}