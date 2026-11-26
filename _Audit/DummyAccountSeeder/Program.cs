using System.Net.Http.Json;
using System.Text.Json;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Qubriq.API.Data;
using Qubriq.API.Models;

const string api = "http://127.0.0.1:5066";

var adminEmail = Environment.GetEnvironmentVariable("QUBRIQ_TEST_ADMIN_EMAIL")!;
var adminPassword = Environment.GetEnvironmentVariable("QUBRIQ_TEST_ADMIN_PASSWORD")!;
var dispatcherEmail = Environment.GetEnvironmentVariable("QUBRIQ_TEST_DISPATCHER_EMAIL")!;
var dispatcherPassword = Environment.GetEnvironmentVariable("QUBRIQ_TEST_DISPATCHER_PASSWORD")!;

if (string.IsNullOrWhiteSpace(adminPassword) ||
    string.IsNullOrWhiteSpace(dispatcherPassword))
    throw new InvalidOperationException("Dummy account passwords are missing.");

if (adminPassword.Length < 12 || dispatcherPassword.Length < 12)
    throw new InvalidOperationException("Dummy passwords must be at least 12 characters.");

var configuration = new ConfigurationBuilder()
    .AddUserSecrets("b170d3c2-39a2-46e0-b1ac-cffab02ad745")
    .Build();

var connectionString = configuration.GetConnectionString("QubriqDatabase");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Database connection string is missing.");

var options = new DbContextOptionsBuilder<QubriqDbContext>()
    .UseMySQL(connectionString)
    .Options;

await using var db = new QubriqDbContext(options);

Console.WriteLine("Checking database and RBAC prerequisites...");

await db.Database.CanConnectAsync();

var adminRole = await db.Roles.SingleOrDefaultAsync(x => x.Name == "Admin")
    ?? throw new InvalidOperationException("Admin role is missing.");

var dispatcherRole = await db.Roles.SingleOrDefaultAsync(
    x => x.Name == "OperationsDispatcher")
    ?? throw new InvalidOperationException("OperationsDispatcher role is missing.");

var allPermissionIds = await db.Permissions.Select(x => x.Id).ToListAsync();

var adminPermissionIds = await db.RolePermissions
    .Where(x => x.RoleId == adminRole.Id && x.IsAllowed)
    .Select(x => x.PermissionId)
    .ToListAsync();

if (allPermissionIds.Count == 0 ||
    allPermissionIds.Except(adminPermissionIds).Any())
    throw new InvalidOperationException(
        "Admin does not have all seeded permissions. Apply and review the RBAC seed first.");

var requiredDispatcherPermissions = new[]
{
    "dashboard.view",
    "bookings.create",
    "bookings.read_all",
    "bookings.update",
    "bookings.cancel",
    "shipments.create",
    "shipments.read_all",
    "shipments.update",
    "shipments.status.update",
    "cargo.manage",
    "fleet.vehicles.manage",
    "fleet.drivers.manage",
    "fleet.assignments.manage",
    "routes.plan",
    "compliance.ewaybill.manage",
    "tracking.view_all",
    "exceptions.read",
    "exceptions.manage",
    "delivery.assignments.manage"
};

var dispatcherAllowed = await db.RolePermissions
    .Where(x => x.RoleId == dispatcherRole.Id && x.IsAllowed)
    .Select(x => x.Permission.Code)
    .ToListAsync();

var missingDispatcherPermissions = requiredDispatcherPermissions
    .Except(dispatcherAllowed)
    .ToArray();

if (missingDispatcherPermissions.Length > 0)
    throw new InvalidOperationException(
        "Dispatcher role is missing permissions: " +
        string.Join(", ", missingDispatcherPermissions));

var forbiddenDispatcherPermissions = new[]
{
    "users.create",
    "users.assign_role",
    "roles.manage",
    "permissions.manage",
    "settings.manage",
    "finance.read",
    "finance.manage"
};

var leakedPermissions = dispatcherAllowed
    .Intersect(forbiddenDispatcherPermissions)
    .ToArray();

if (leakedPermissions.Length > 0)
    throw new InvalidOperationException(
        "Dispatcher role has unexpected elevated permissions: " +
        string.Join(", ", leakedPermissions));

var emails = new[] { adminEmail, dispatcherEmail };

if (emails.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2)
    throw new InvalidOperationException("Test account emails must be different.");

var existing = await db.Users
    .Where(x => emails.Contains(x.Email))
    .Select(x => x.Email)
    .ToListAsync();

if (existing.Count > 0)
    throw new InvalidOperationException(
        "Existing test email(s) found. No records changed: " +
        string.Join(", ", existing));

var now = DateTime.UtcNow;
var hasher = new PasswordHasher<User>();

var admin = new User
{
    FullName = "Qubriq Dummy Administrator",
    Email = adminEmail,
    IsActive = true,
    EmailVerified = true,
    RecipientType = null,
    CreatedAtUtc = now,
    UpdatedAtUtc = now
};

var dispatcher = new User
{
    FullName = "Qubriq Dummy Dispatcher",
    Email = dispatcherEmail,
    IsActive = true,
    EmailVerified = true,
    RecipientType = null,
    CreatedAtUtc = now,
    UpdatedAtUtc = now
};

admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
dispatcher.PasswordHash = hasher.HashPassword(dispatcher, dispatcherPassword);

await using var transaction = await db.Database.BeginTransactionAsync(
    IsolationLevel.Serializable);

try
{
    db.Users.AddRange(admin, dispatcher);
    await db.SaveChangesAsync();

    db.UserRoles.AddRange(
        new UserRole
        {
            UserId = admin.Id,
            RoleId = adminRole.Id,
            AssignedAtUtc = now
        },
        new UserRole
        {
            UserId = dispatcher.Id,
            RoleId = dispatcherRole.Id,
            AssignedAtUtc = now
        });

    db.AuditLogs.AddRange(
        new AuditLog
        {
            UserId = admin.Id,
            Action = "DEVELOPMENT_TEST_ADMIN_CREATED",
            EntityType = "User",
            EntityId = admin.Id.ToString(),
            Details = "Local development test account; Admin role assigned.",
            CreatedAtUtc = now
        },
        new AuditLog
        {
            UserId = admin.Id,
            Action = "DEVELOPMENT_TEST_DISPATCHER_CREATED",
            EntityType = "User",
            EntityId = dispatcher.Id.ToString(),
            Details = "Local development test account; OperationsDispatcher role assigned.",
            CreatedAtUtc = now
        });

    await db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

Console.WriteLine("Database account creation committed.");

using var client = new HttpClient();

async Task VerifyLogin(string email, string password, string expectedRole)
{
    var response = await client.PostAsJsonAsync(
        api + "/api/auth/login",
        new { Email = email, Password = password });

    var responseText = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
        throw new InvalidOperationException(
            $"Login verification failed for {email}: HTTP {(int)response.StatusCode}. " +
            responseText);

    using var document = JsonDocument.Parse(responseText);
    var root = document.RootElement;

    if (!root.TryGetProperty("user", out var user) ||
        !user.TryGetProperty("roles", out var roles))
        throw new InvalidOperationException(
            "Login response did not contain user.roles.");

    var actualRoles = roles.EnumerateArray()
        .Select(x => x.GetString())
        .Where(x => x != null)
        .ToArray();

    if (!actualRoles.Contains(expectedRole))
        throw new InvalidOperationException(
            $"Expected role {expectedRole}; received: " +
            string.Join(", ", actualRoles));

    Console.WriteLine($"PASS: {email} authenticated with role {expectedRole}.");
}

await VerifyLogin(adminEmail, adminPassword, "Admin");
await VerifyLogin(dispatcherEmail, dispatcherPassword, "OperationsDispatcher");

Console.WriteLine();
Console.WriteLine("===== VERIFIED TEST ACCOUNTS =====");
Console.WriteLine($"Admin ID: {admin.Id} | {adminEmail}");
Console.WriteLine($"Dispatcher ID: {dispatcher.Id} | {dispatcherEmail}");
Console.WriteLine("Both passwords are stored as ASP.NET Core PasswordHasher hashes.");
Console.WriteLine("Existing users and RBAC permission records were not modified.");
