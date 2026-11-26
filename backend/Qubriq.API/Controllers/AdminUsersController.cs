using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs.Admin;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly QubriqDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public AdminUsersController(QubriqDbContext db)
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

        if (!id.HasValue)
            return false;

        return await _db.UserRoles
            .Where(x => x.UserId == id.Value)
            .Join(
                _db.RolePermissions,
                userRole => userRole.RoleId,
                rolePermission => rolePermission.RoleId,
                (userRole, rolePermission) => rolePermission)
            .AnyAsync(x =>
                x.IsAllowed &&
                x.Permission.Code == code);
    }

    [HttpPost("operations-dispatcher")]
    public async Task<IActionResult> CreateOperationsDispatcher(
        [FromBody] CreateOperationsDispatcherRequest request)
    {
        var accountPolicy = await _db.AccountRegistrationPolicies.FirstOrDefaultAsync(x => x.Id == 1);
        if (accountPolicy == null || !accountPolicy.AdminAccountCreationEnabled)
            return StatusCode(403, new { message = "Administrator account creation is disabled by policy." });
        if (!await HasPermission("users.create") ||
            !await HasPermission("users.assign_role"))
        {
            return Forbid();
        }

        if (request == null)
        {
            return BadRequest(new
            {
                message = "Dispatcher details are required."
            });
        }

        var adminId = AdminId();

        if (!adminId.HasValue)
        {
            return Forbid();
        }

        var name = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
        {
            return BadRequest(new
            {
                message = "Name is required and must not exceed 150 characters."
            });
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
        {
            return BadRequest(new
            {
                message = "A valid email address is required."
            });
        }

        try
        {
            var parsedEmail = new System.Net.Mail.MailAddress(email);

            if (!string.Equals(
                parsedEmail.Address,
                email,
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "A valid email address is required."
                });
            }
        }
        catch (FormatException)
        {
            return BadRequest(new
            {
                message = "A valid email address is required."
            });
        }

        if (password.Length < 12 || password.Length > 128)
        {
            return BadRequest(new
            {
                message = "Password must be 12-128 characters."
            });
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            if (await _db.Users.AnyAsync(x => x.Email == email))
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = "Email already exists."
                });
            }

            var role = await _db.Roles
                .FirstOrDefaultAsync(x => x.Name == "OperationsDispatcher");

            if (role == null)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "OperationsDispatcher role is missing. Verify RBAC seed data."
                });
            }

            var user = new User
            {
                FullName = name,
                Email = email,
                PasswordHash = null,
                RecipientType = null,
                IsActive = true,
                EmailVerified = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            user.PasswordHash = _hasher.HashPassword(user, password);

            _db.Users.Add(user);

            await _db.SaveChangesAsync();

            _db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAtUtc = DateTime.UtcNow
            });

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = adminId.Value,
                Action = "OPERATIONS_DISPATCHER_ACCOUNT_PROVISIONED",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                Details = $"Role=OperationsDispatcher; Email={email}",
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Created(
                $"/api/admin/users/{user.Id}",
                new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    Role = "OperationsDispatcher",
                    user.IsActive,
                    user.EmailVerified
                });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "Account creation conflicted with existing database data."
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

