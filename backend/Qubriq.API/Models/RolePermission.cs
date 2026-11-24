namespace Qubriq.API.Models;

public class RolePermission
{
    public long RoleId { get; set; }

    public long PermissionId { get; set; }

    public bool IsAllowed { get; set; } = true;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public Role Role { get; set; } = null!;

    public Permission Permission { get; set; } = null!;
}