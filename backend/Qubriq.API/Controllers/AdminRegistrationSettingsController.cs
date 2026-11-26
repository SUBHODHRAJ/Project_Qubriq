using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs.Admin;
using Qubriq.API.Models;
namespace Qubriq.API.Controllers;
[ApiController, Route("api/admin/settings/registration"), Authorize(Roles = "Admin")]
public sealed class AdminRegistrationSettingsController : ControllerBase
{
    private readonly QubriqDbContext _db;
    public AdminRegistrationSettingsController(QubriqDbContext db) => _db = db;
    private async Task<bool> HasUserAdminPermissions()
    {
        var id = AdminId();
        if (!id.HasValue) return false;

        var permissionCodes = await _db.UserRoles
            .Where(ur => ur.UserId == id.Value)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .Where(rp => rp.IsAllowed)
            .Select(rp => rp.Permission.Code)
            .ToListAsync();

        return permissionCodes.Contains("users.create")
            && permissionCodes.Contains("users.assign_role");
    }
    private long? AdminId() {
        var v = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(v, out var id) ? id : null;
    }
    [HttpGet]
    public async Task<IActionResult> Get() {
        if (!await HasUserAdminPermissions()) return Forbid();
        var p = await _db.AccountRegistrationPolicies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
        return p == null ? StatusCode(503, new { message = "Registration policy is not initialized." }) : Ok(p);
    }
    [HttpPut]
    public async Task<IActionResult> Update(UpdateRegistrationPolicyRequest r) {
        if (!await HasUserAdminPermissions()) return Forbid();
        var id = AdminId(); if (!id.HasValue) return Unauthorized();
        var p = await _db.AccountRegistrationPolicies.FirstOrDefaultAsync(x => x.Id == 1);
        if (p == null) return StatusCode(503, new { message = "Registration policy is not initialized." });
        p.PublicRegistrationEnabled = r.PublicRegistrationEnabled;
        p.BookingCustomerSignupEnabled = r.BookingCustomerSignupEnabled;
        p.FinalRecipientSignupEnabled = r.FinalRecipientSignupEnabled;
        p.RequireAdminApproval = r.RequireAdminApproval;
        p.ActivatePublicAccountsImmediately = r.ActivatePublicAccountsImmediately;
        p.AdminAccountCreationEnabled = r.AdminAccountCreationEnabled;
        p.UpdatedAtUtc = DateTime.UtcNow; p.UpdatedByUserId = id;
        _db.AuditLogs.Add(new AuditLog {
            UserId = id, Action = "REGISTRATION_POLICY_UPDATED", EntityType = "AccountRegistrationPolicy",
            EntityId = "1", Details = System.Text.Json.JsonSerializer.Serialize(r), CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Registration policy updated.", policy = p });
    }
}

