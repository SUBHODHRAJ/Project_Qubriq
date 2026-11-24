using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs.Auth;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/auth/bootstrap")]
public class AdminBootstrapController : ControllerBase
{
    private readonly QubriqDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AdminBootstrapController(
        QubriqDbContext db,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _db = db;
        _configuration = configuration;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpPost("admin")]
    public async Task<IActionResult> CreateFirstAdmin(
        [FromBody] BootstrapAdminRequest request)
    {
        // Never expose bootstrap outside Development.
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var configuredSecret = _configuration["Bootstrap:Secret"];
        var suppliedSecret = Request.Headers["X-Qubriq-Bootstrap-Key"].ToString();

        if (string.IsNullOrWhiteSpace(configuredSecret) ||
            string.IsNullOrWhiteSpace(suppliedSecret))
        {
            return Unauthorized(new
            {
                message = "Invalid bootstrap credentials."
            });
        }

        var expectedBytes = Encoding.UTF8.GetBytes(configuredSecret);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedSecret);

        if (expectedBytes.Length != suppliedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(
                expectedBytes,
                suppliedBytes))
        {
            return Unauthorized(new
            {
                message = "Invalid bootstrap credentials."
            });
        }

        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Full name and email are required."
            });
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            // Bootstrap is permitted only for a completely empty user table.
            var existingUser = await _db.Users.AnyAsync();

            if (existingUser)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = "Bootstrap is disabled because a user account already exists."
                });
            }

            var adminRole = await _db.Roles
                .FirstOrDefaultAsync(x => x.Name == "Admin");

            if (adminRole == null)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Admin role is missing. Verify RBAC seed data."
                });
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                EmailVerified = true,
                PasswordHash = null,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var passwordHasher = new PasswordHasher<User>();

            user.PasswordHash = passwordHasher.HashPassword(
                user,
                request.Password);

            _db.Users.Add(user);

            await _db.SaveChangesAsync();

            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id,
                AssignedAtUtc = DateTime.UtcNow
            };

            _db.UserRoles.Add(userRole);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Created(
                "/api/auth/login",
                new
                {
                    message = "Initial Admin account created successfully.",
                    userId = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    role = "Admin",
                    nextStep = "Sign in using POST /api/auth/login."
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
    }
}
