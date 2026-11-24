using Microsoft.AspNetCore.Authorization;

namespace Qubriq.API.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyCollection<string> PermissionCodes { get; }

    public PermissionRequirement(params string[] permissionCodes)
    {
        PermissionCodes = permissionCodes;
    }
}
