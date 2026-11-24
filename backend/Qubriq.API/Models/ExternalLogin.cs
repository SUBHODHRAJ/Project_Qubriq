namespace Qubriq.API.Models;

public class ExternalLogin
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Provider { get; set; } = "Google";

    public string ProviderSubject { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}