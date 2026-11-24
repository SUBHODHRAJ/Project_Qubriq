using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/dispatcher")]
[Authorize(Roles = "Admin,OperationsDispatcher")]
public class DispatcherController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public DispatcherController(QubriqDbContext db)
    {
        _db = db;
    }

    private long? GetUserId()
    {
        var value = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(value, out var id) ? id : null;
    }

    private async Task<bool> HasPermissionAsync(string code)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return false;

        var active = await _db.Users.AnyAsync(
            x => x.Id == userId.Value && x.IsActive);

        if (!active) return false;

        return await _db.UserRoles
            .Where(x => x.UserId == userId.Value)
            .Join(
                _db.RolePermissions,
                userRole => userRole.RoleId,
                rolePermission => rolePermission.RoleId,
                (_, rolePermission) => rolePermission)
            .AnyAsync(x =>
                x.IsAllowed &&
                x.Permission.Code == code);
    }

    // GET: api/dispatcher/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        if (!await HasPermissionAsync("dashboard.view"))
            return Forbid();

        var totalBookings = await _db.Shipments.CountAsync();

        var draftBookings = await _db.Shipments
            .CountAsync(x => x.Status == "Draft");

        var confirmedBookings = await _db.Shipments
            .CountAsync(x => x.Status == "Confirmed");

        var rejectedBookings = await _db.Shipments
            .CountAsync(x => x.Status == "Rejected");

        var activeShipments = await _db.Shipments
            .CountAsync(x =>
                x.Status == "Assigned" ||
                x.Status == "PickedUp" ||
                x.Status == "InTransit");

        var deliveredShipments = await _db.Shipments
            .CountAsync(x => x.Status == "Delivered");

        var availableVehicles = await _db.Vehicles
            .CountAsync(x => x.Status == "Available");

        var availableDrivers = await _db.Drivers
            .CountAsync(x => x.Status == "Available");

        var activeAssignments = await _db.ShipmentAssignments
            .CountAsync(x => x.Status == "Active");

        return Ok(new
        {
            bookings = new
            {
                total = totalBookings,
                pending = draftBookings,
                confirmed = confirmedBookings,
                rejected = rejectedBookings
            },
            shipments = new
            {
                active = activeShipments,
                delivered = deliveredShipments
            },
            fleet = new
            {
                availableVehicles,
                availableDrivers,
                activeAssignments
            },
            generatedAtUtc = DateTime.UtcNow
        });
    }

    // GET: api/dispatcher/shipments
    [HttpGet("shipments")]
    public async Task<IActionResult> GetShipments(
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        if (!await HasPermissionAsync("shipments.read_all"))
            return Forbid();

        var query = _db.Shipments
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.TrackingNumber.Contains(term) ||
                x.Origin.Contains(term) ||
                x.Destination.Contains(term) ||
                x.Customer.CompanyName.Contains(term));
        }

        var result = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                trackingNumber = x.TrackingNumber,
                customer = x.Customer.CompanyName,
                origin = x.Origin,
                destination = x.Destination,
                cargoName = x.CargoName,
                cargoCategory = x.CargoCategory,
                packageCount = x.PackageCount,
                weightKg = x.TotalWeightKg,
                priority = x.Priority,
                pickupDateUtc = x.PickupDateUtc,
                expectedDeliveryUtc = x.ExpectedDeliveryUtc,
                status = x.Status,
                createdAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(result);
    }

    // PATCH: api/dispatcher/bookings/{id}/status
    [HttpPatch("bookings/{id:long}/status")]
    public async Task<IActionResult> UpdateBookingStatus(
        long id,
        [FromBody] BookingStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Status is required." });

        var target = request.Status.Trim();

        if (target is not ("Confirmed" or "Rejected" or "Cancelled"))
            return BadRequest(new
            {
                message = "Allowed statuses: Confirmed, Rejected, Cancelled."
            });

        var requiredPermission = target == "Cancelled"
            ? "bookings.cancel"
            : "bookings.update";

        if (!await HasPermissionAsync(requiredPermission))
            return Forbid();

        var booking = await _db.Shipments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (booking == null)
            return NotFound(new { message = "Booking not found." });

        var allowed =
            booking.Status == "Draft" &&
            target is "Confirmed" or "Rejected" or "Cancelled";

        if (booking.Status == "Confirmed" && target == "Cancelled")
            allowed = true;

        if (!allowed)
            return Conflict(new
            {
                message = $"Cannot change booking status from {booking.Status} to {target}."
            });

        var previousStatus = booking.Status;
        booking.Status = target;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            booking.Id,
            booking.TrackingNumber,
            previousStatus,
            currentStatus = booking.Status,
            message = "Booking status updated successfully."
        });
    }

    // PATCH: api/dispatcher/shipments/{id}/status
    [HttpPatch("shipments/{id:long}/status")]
    public async Task<IActionResult> UpdateShipmentStatus(
        long id,
        [FromBody] ShipmentStatusRequest request)
    {
        if (!await HasPermissionAsync("shipments.status.update"))
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Status is required." });

        var target = request.Status.Trim();

        var allowedStatuses = new[]
        {
            "PickedUp",
            "InTransit",
            "Delivered"
        };

        if (!allowedStatuses.Contains(target))
            return BadRequest(new
            {
                message = "Allowed statuses: PickedUp, InTransit, Delivered."
            });

        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shipment == null)
            return NotFound(new { message = "Shipment not found." });

        var validTransition =
            (shipment.Status == "Assigned" && target == "PickedUp") ||
            (shipment.Status == "PickedUp" && target == "InTransit") ||
            (shipment.Status == "InTransit" && target == "Delivered");

        if (!validTransition)
            return Conflict(new
            {
                message = $"Invalid shipment transition: {shipment.Status} to {target}."
            });

        var previousStatus = shipment.Status;
        shipment.Status = target;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            shipment.Id,
            shipment.TrackingNumber,
            previousStatus,
            currentStatus = shipment.Status,
            message = "Shipment status updated successfully."
        });
    }
}

public sealed class BookingStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed class ShipmentStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
