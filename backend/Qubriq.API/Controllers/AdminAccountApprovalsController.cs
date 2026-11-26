using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;
namespace Qubriq.API.Controllers;
[ApiController, Route("api/admin/users"), Authorize(Roles = "Admin")]
public sealed class AdminAccountApprovalsController : ControllerBase
{
    private readonly QubriqDbContext _db;
    public AdminAccountApprovalsController(QubriqDbContext db) => _db = db;
    private long? AdminId() {
        var v = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(v, out var id) ? id : null;
    }
    private async Task<bool> HasUserAdminPermissions() {
        var id = AdminId(); if (!id.HasValue) return false;
        return await _db.UserRoles.Where(x => x.UserId == id.Value)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .AnyAsync(x => x.IsAllowed && x.Permission.Code == "users.create")
            && await _db.UserRoles.Where(x => x.UserId == id.Value)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .AnyAsync(x => x.IsAllowed && x.Permission.Code == "users.assign_role");
    }
    [HttpGet("pending")]
    public async Task<IActionResult> Pending() {
        if (!await HasUserAdminPermissions()) return Forbid();
        var list = await _db.Users.Where(x => x.RequiresAdminApproval && !x.IsActive)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.FullName, x.Email, x.RecipientType, x.CreatedAtUtc,
                Roles = x.UserRoles.Select(ur => ur.Role.Name).ToList() }).ToListAsync();
        return Ok(list);
    }
    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id) {
        if (!await HasUserAdminPermissions()) return Forbid();
        var user = await _db.Users.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound();
        if (!user.RequiresAdminApproval || user.IsActive) return Conflict(new { message = "Account is not awaiting approval." });
        user.IsActive = true; user.RequiresAdminApproval = false; user.UpdatedAtUtc = DateTime.UtcNow;
        if (user.Customer != null) user.Customer.IsActive = true;
        _db.AuditLogs.Add(new AuditLog { UserId = AdminId(), Action = "USER_ACCOUNT_APPROVED",
            EntityType = "User", EntityId = id.ToString(), Details = "Account approved by Admin.",
            CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Account approved.", userId = id });
    }
}

