using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs.Auth;
using Qubriq.API.Models;
using Qubriq.API.Services;
namespace Qubriq.API.Controllers;
[ApiController]
[Route("api/auth")]
public sealed class RegistrationController : ControllerBase
{
    private readonly QubriqDbContext _db;
    private readonly RegistrationPolicyService _policies;
    private readonly PasswordHasher<User> _hasher = new();
    public RegistrationController(QubriqDbContext db, RegistrationPolicyService policies)
    { _db = db; _policies = policies; }

    [AllowAnonymous, HttpGet("registration-policy")]
    public async Task<IActionResult> PublicPolicy(CancellationToken ct)
    {
        var p = await _policies.GetAsync(ct);
        if (p == null) return StatusCode(503, new { message = "Registration policy is not initialized. Apply the migration first." });
        var types = new List<string>();
        if (p.PublicRegistrationEnabled && p.BookingCustomerSignupEnabled) types.Add("BookingCustomer");
        if (p.PublicRegistrationEnabled && p.FinalRecipientSignupEnabled) types.Add("FinalRecipient");
        return Ok(new { publicRegistrationEnabled = p.PublicRegistrationEnabled, accountTypes = types });
    }

    [AllowAnonymous, HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] PublicRegistrationRequest r, CancellationToken ct)
    {
        var p = await _policies.GetAsync(ct);
        if (p == null) return StatusCode(503, new { message = "Registration policy is not initialized." });
        if (!p.PublicRegistrationEnabled) return StatusCode(403, new { message = "Public registration is disabled." });

        var type = r.AccountType.Trim();
        if (type == "BookingCustomer" && !p.BookingCustomerSignupEnabled ||
            type == "FinalRecipient" && !p.FinalRecipientSignupEnabled)
            return StatusCode(403, new { message = "This account type is not currently available." });
        if (type != "BookingCustomer" && type != "FinalRecipient")
            return BadRequest(new { message = "Unsupported public account type." });

        var name = r.FullName.Trim();
        var email = r.Email.Trim().ToLowerInvariant();
        if (type == "BookingCustomer" && string.IsNullOrWhiteSpace(r.CompanyName))
            return BadRequest(new { message = "Company name is required." });
        try {
            if (!string.Equals(new System.Net.Mail.MailAddress(email).Address, email, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Invalid email address." });
        } catch (FormatException) { return BadRequest(new { message = "Invalid email address." }); }

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (await _db.Users.AnyAsync(x => x.Email == email, ct))
            return Conflict(new { message = "An account with this email already exists." });
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == type, ct);
        if (role == null) return StatusCode(500, new { message = "Required RBAC role is missing." });

        var now = DateTime.UtcNow;
        var pending = p.RequireAdminApproval || !p.ActivatePublicAccountsImmediately;
        var user = new User {
            FullName = name, Email = email, IsActive = !pending, EmailVerified = false, RequiresAdminApproval = pending,
            RecipientType = type == "FinalRecipient" ? "FinalRecipient" : null,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
        user.PasswordHash = _hasher.HashPassword(user, r.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAtUtc = now });

        if (type == "BookingCustomer") {
            _db.Customers.Add(new Customer {
                UserId = user.Id, CompanyName = r.CompanyName!.Trim(),
                ContactName = string.IsNullOrWhiteSpace(r.ContactName) ? name : r.ContactName.Trim(),
                Email = email, Phone = r.Phone?.Trim(), Address = r.Address?.Trim(),
                IsActive = !pending, CreatedAtUtc = now
            });
        }
        _db.AuditLogs.Add(new AuditLog {
            UserId = user.Id, Action = "PUBLIC_ACCOUNT_REGISTERED", EntityType = "User",
            EntityId = user.Id.ToString(), Details = "AccountType=" + type + "; PendingApproval=" + pending,
            CreatedAtUtc = now
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Created("/api/auth/login", new {
            message = pending ? "Registration received. Await administrator approval." : "Account created. You can now sign in.",
            userId = user.Id, accountType = type, requiresApproval = pending
        });
    }
}

