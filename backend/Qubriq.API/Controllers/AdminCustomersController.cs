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
[Route("api/admin/customers")]
[Authorize(Policy = "AdminCustomerProvision")]
public class AdminCustomersController : ControllerBase
{
    private readonly QubriqDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher;

    public AdminCustomersController(QubriqDbContext db)
    {
        _db = db;
        _passwordHasher = new PasswordHasher<User>();
    }

    private long? GetAuthenticatedUserId()
    {
        var subject =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (long.TryParse(subject, out var userId))
        {
            return userId;
        }

        return null;
    }

    // POST: api/admin/customers/{customerId}/account
    [HttpPost("{customerId:long}/account")]
    public async Task<IActionResult> CreateCustomerAccount(
        long customerId,
        [FromBody] CreateCustomerAccountRequest request)
    {
        var adminUserId = GetAuthenticatedUserId();

        if (!adminUserId.HasValue)
        {
            return Unauthorized();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Full name, email and password are required."
            });
        }

        if (request.Password.Length < 12 ||
            request.Password.Length > 128)
        {
            return BadRequest(new
            {
                message = "Password must contain between 12 and 128 characters."
            });
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var customer = await _db.Customers
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer == null)
        {
            return NotFound(new
            {
                message = "Customer record was not found."
            });
        }

        if (!customer.IsActive)
        {
            return BadRequest(new
            {
                message = "An inactive customer cannot receive a login account."
            });
        }

        if (customer.UserId.HasValue)
        {
            return Conflict(new
            {
                message = "This customer already has a linked user account.",
                customerId = customer.Id
            });
        }

        var existingUser = await _db.Users
            .AnyAsync(x => x.Email == email);

        if (existingUser)
        {
            return Conflict(new
            {
                message = "An account with this email address already exists."
            });
        }

        var customerRole = await _db.Roles
            .FirstOrDefaultAsync(x => x.Name == "BookingCustomer");

        if (customerRole == null)
        {
            return StatusCode(500, new
            {
                message = "BookingCustomer role is missing. Check RBAC seed data."
            });
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            IsActive = true,
            EmailVerified = false,
            RecipientType = null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = customerRole.Id,
            AssignedAtUtc = DateTime.UtcNow
        };

        _db.UserRoles.Add(userRole);

        customer.UserId = user.Id;

        var auditLog = new AuditLog
        {
            UserId = adminUserId.Value,
            Action = "CUSTOMER_ACCOUNT_PROVISIONED",
            EntityType = "Customer",
            EntityId = customer.Id.ToString(),
            Details =
                $"Created BookingCustomer account. UserId={user.Id}; Email={email}",
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.AuditLogs.Add(auditLog);

        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Customer account created and linked successfully.",
            customer = new
            {
                customer.Id,
                customer.CompanyName,
                customer.UserId
            },
            account = new
            {
                user.Id,
                user.FullName,
                user.Email,
                role = customerRole.Name,
                user.IsActive,
                user.EmailVerified
            }
        });
    }
}
