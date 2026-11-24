using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/admin/shipment-assignments")]
[Authorize(Roles = "Admin,OperationsDispatcher")]
public class AdminShipmentAssignmentsController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public AdminShipmentAssignmentsController(QubriqDbContext db)
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
                (_, p) => p)
            .AnyAsync(x => x.IsAllowed && x.Permission.Code == code);
    }

    public sealed class DriverAssignmentRequest
    {
        public long ShipmentId { get; set; }
        public long DriverId { get; set; }
        public long VehicleId { get; set; }
    }

    [HttpPost("driver")]
    public async Task<IActionResult> AssignDriver(
        [FromBody] DriverAssignmentRequest request)
    {
        if (!await HasPermission("delivery.assignments.manage"))
            return Forbid();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(x => x.Id == request.ShipmentId);

        if (shipment == null)
            return NotFound(new { message = "Shipment not found." });

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(x => x.Id == request.DriverId);

        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(x => x.Id == request.VehicleId);

        if (driver == null || vehicle == null)
            return BadRequest(new { message = "Valid driver and vehicle are required." });

        if (driver.Status != "Available" || vehicle.Status != "Available")
            return Conflict(new { message = "Driver and vehicle must be Available." });

        var shipmentAlreadyAssigned = await _db.ShipmentAssignments.AnyAsync(x =>
            x.ShipmentId == request.ShipmentId &&
            x.Status == "Active" &&
            x.ReleasedAtUtc == null);

        if (shipmentAlreadyAssigned)
            return Conflict(new { message = "Shipment already has an active assignment." });

        var driverAlreadyAssigned = await _db.ShipmentAssignments.AnyAsync(x =>
            x.DriverId == request.DriverId &&
            x.Status == "Active" &&
            x.ReleasedAtUtc == null);

        if (driverAlreadyAssigned)
            return Conflict(new { message = "Driver already has an active assignment." });

        var vehicleAlreadyAssigned = await _db.ShipmentAssignments.AnyAsync(x =>
            x.VehicleId == request.VehicleId &&
            x.Status == "Active" &&
            x.ReleasedAtUtc == null);

        if (vehicleAlreadyAssigned)
            return Conflict(new { message = "Vehicle already has an active assignment." });

        var assignment = new ShipmentAssignment
        {
            ShipmentId = shipment.Id,
            DriverId = driver.Id,
            VehicleId = vehicle.Id,
            AssignedAtUtc = DateTime.UtcNow,
            Status = "Active"
        };

        _db.ShipmentAssignments.Add(assignment);

        shipment.Status = "Assigned";

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = AdminId(),
            Action = "SHIPMENT_DRIVER_ASSIGNED",
            EntityType = "Shipment",
            EntityId = shipment.Id.ToString(),
            Details = $"AssignmentId={assignment.Id}; DriverId={driver.Id}; VehicleId={vehicle.Id}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            assignment.Id,
            assignment.ShipmentId,
            assignment.DriverId,
            assignment.VehicleId,
            shipment.Status
        });
    }

    public sealed class FinalRecipientRequest
    {
        public long ShipmentId { get; set; }
        public long RecipientUserId { get; set; }
    }

    [HttpPost("recipient")]
    public async Task<IActionResult> AssignFinalRecipient(
        [FromBody] FinalRecipientRequest request)
    {
        if (!await HasPermission("delivery.assignments.manage"))
            return Forbid();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var user = await _db.Users
            .FirstOrDefaultAsync(x =>
                x.Id == request.RecipientUserId &&
                x.IsActive &&
                x.RecipientType == "FinalRecipient");

        if (user == null)
            return BadRequest(new { message = "Active FinalRecipient account not found." });

        var shipmentExists = await _db.Shipments
            .AnyAsync(x => x.Id == request.ShipmentId);

        if (!shipmentExists)
            return NotFound(new { message = "Shipment not found." });

        if (await _db.ShipmentRecipients.AnyAsync(x =>
            x.ShipmentId == request.ShipmentId &&
            x.UserId == request.RecipientUserId))
            return Conflict(new { message = "Recipient is already linked to this shipment." });

        _db.ShipmentRecipients.Add(new ShipmentRecipient
        {
            ShipmentId = request.ShipmentId,
            UserId = request.RecipientUserId,
            CreatedAtUtc = DateTime.UtcNow
        });

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = AdminId(),
            Action = "FINAL_RECIPIENT_ASSIGNED",
            EntityType = "Shipment",
            EntityId = request.ShipmentId.ToString(),
            Details = $"RecipientUserId={request.RecipientUserId}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { message = "Final recipient linked to shipment." });
    }
}
