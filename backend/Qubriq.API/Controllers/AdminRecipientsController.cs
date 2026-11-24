using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/admin/recipients")]
[Authorize(Roles = "Admin")]
public class AdminRecipientsController : ControllerBase
{
    private readonly QubriqDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public AdminRecipientsController(QubriqDbContext db)
    {
        _db = db;
    }

    private long? AdminId()
    {
        var value = User.FindFirst("sub")?.Value ??
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(value, out var id) ? id : null;
    }

    private async Task<bool> HasPermission(string code)
    {
        var id = AdminId();
        if (!id.HasValue) return false;

        return await _db.UserRoles
            .Where(x => x.UserId == id.Value)
            .Join(_db.RolePermissions,
                x => x.RoleId,
                x => x.RoleId,
                (_, permission) => permission)
            .AnyAsync(x =>
                x.IsAllowed && x.Permission.Code == code);
    }

    public sealed class CreateRecipientRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string RecipientType { get; set; } = "";
        public long? DriverId { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipient(
        [FromBody] CreateRecipientRequest request)
    {
        if (!await HasPermission("users.create") ||
            !await HasPermission("users.assign_role"))
            return Forbid();

        if (request == null)
            return BadRequest(new { message = "Recipient details are required." });

        var name = request.FullName?.Trim() ?? "";
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var password = request.Password;
        var type = request.RecipientType?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
            return BadRequest(new { message = "Name is required and must not exceed 150 characters." });

        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
            return BadRequest(new { message = "A valid email address is required." });

        try
        {
            var parsedEmail = new System.Net.Mail.MailAddress(email);

            if (!string.Equals(
                parsedEmail.Address,
                email,
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "A valid email address is required." });
            }
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "A valid email address is required." });
        }

        if (string.IsNullOrEmpty(password) ||
            password.Length < 12 ||
            password.Length > 128)
        {
            return BadRequest(new { message = "Password must be 12-128 characters." });
        }

        if (type != "DeliveryPersonnel" && type != "FinalRecipient")
            return BadRequest(new { message = "Invalid recipient subtype." });

        if (type == "DeliveryPersonnel" && !request.DriverId.HasValue)
            return BadRequest(new { message = "DriverId is required for DeliveryPersonnel." });

        if (type == "FinalRecipient" && request.DriverId.HasValue)
            return BadRequest(new { message = "FinalRecipient cannot be linked to a Driver." });

        await using var tx = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);

        if (await _db.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "Email already exists." });

        Driver? driver = null;

        if (type == "DeliveryPersonnel")
        {
            driver = await _db.Drivers
                .FirstOrDefaultAsync(x => x.Id == request.DriverId.GetValueOrDefault());

            if (driver == null)
                return NotFound(new { message = "Driver not found." });

            if (driver.UserId.HasValue)
                return Conflict(new { message = "Driver already has a linked user." });
        }

        var role = await _db.Roles
            .FirstOrDefaultAsync(x => x.Name == "ShipmentRecipient");

        if (role == null)
            return StatusCode(500, new { message = "ShipmentRecipient role is missing." });

        var user = new User
        {
            FullName = name,
            Email = email,
            RecipientType = type,
            IsActive = true,
            EmailVerified = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAtUtc = DateTime.UtcNow
        });

        if (driver != null)
            driver.UserId = user.Id;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = AdminId(),
            Action = "RECIPIENT_ACCOUNT_PROVISIONED",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"RecipientType={type}; DriverId={driver?.Id}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Created("", new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.RecipientType,
            user.IsActive,
            DriverId = driver?.Id
        });
    }
}
