using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;

namespace Qubriq.API.Authorization;

public class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly QubriqDbContext _db;

    public PermissionAuthorizationHandler(QubriqDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var subject =
            context.User.FindFirst("sub")?.Value ??
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!long.TryParse(subject, out var userId))
        {
            return;
        }

        var isActive = await _db.Users
            .AnyAsync(user => user.Id == userId && user.IsActive);

        if (!isActive)
        {
            return;
        }

        var hasPermission = await _db.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(
                _db.RolePermissions,
                userRole => userRole.RoleId,
                rolePermission => rolePermission.RoleId,
                (userRole, rolePermission) => rolePermission)
            .AnyAsync(rolePermission =>
                rolePermission.IsAllowed &&
                requirement.PermissionCodes.Contains(
                    rolePermission.Permission.Code));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
