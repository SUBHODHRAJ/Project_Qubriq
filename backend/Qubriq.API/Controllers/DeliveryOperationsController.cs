using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/operations/deliveries")]
[Authorize(Roles = "Admin,OperationsDispatcher")]
public class DeliveryOperationsController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public DeliveryOperationsController(QubriqDbContext db)
    {
        _db = db;
    }

    private long? UserId()
    {
        var value = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(value, out var id) ? id : null;
    }

    private async Task<bool> Allowed(string permission)
    {
        var id = UserId();
        if (!id.HasValue) return false;

        if (!await _db.Users.AnyAsync(x => x.Id == id.Value && x.IsActive))
            return false;

        return await _db.UserRoles
            .Where(x => x.UserId == id.Value)
            .Join(
                _db.RolePermissions,
                ur => ur.RoleId,
                rp => rp.RoleId,
                (_, rp) => rp)
            .AnyAsync(x =>
                x.IsAllowed &&
                x.Permission.Code == permission);
    }

    [HttpGet]
    public async Task<IActionResult> GetDeliveries(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        if (!await Allowed("shipments.read_all"))
            return Forbid();

        var query =
            from assignment in _db.ShipmentAssignments.AsNoTracking()
            join shipment in _db.Shipments.AsNoTracking()
                on assignment.ShipmentId equals shipment.Id
            join driver in _db.Drivers.AsNoTracking()
                on assignment.DriverId equals driver.Id
            join vehicle in _db.Vehicles.AsNoTracking()
                on assignment.VehicleId equals vehicle.Id
            where assignment.Status == "Active"
            select new
            {
                assignment.Id,
                assignment.ShipmentId,
                assignment.DriverId,
                assignment.VehicleId,
                assignment.AssignedAtUtc,
                AssignmentStatus = assignment.Status,
                shipment.TrackingNumber,
                shipment.Origin,
                shipment.Destination,
                ShipmentStatus = shipment.Status,
                shipment.ExpectedDeliveryUtc,
                DriverName = driver.FullName,
                DriverPhone = driver.Phone,
                VehicleRegistration = vehicle.RegistrationNumber,
                VehicleType = vehicle.VehicleType
            };

        var rows = await query
            .OrderByDescending(x => x.AssignedAtUtc)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(status))
        {
            rows = rows
                .Where(x => string.Equals(
                    x.ShipmentStatus,
                    status.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            rows = rows.Where(x =>
                (x.TrackingNumber ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (x.Origin ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (x.Destination ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (x.DriverName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (x.VehicleRegistration ?? "").Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(new
        {
            totalActiveAssignments = rows.Count,
            assigned = rows.Count(x => x.ShipmentStatus == "Assigned"),
            pickedUp = rows.Count(x => x.ShipmentStatus == "PickedUp"),
            inTransit = rows.Count(x => x.ShipmentStatus == "InTransit"),
            deliveries = rows
        });
    }
}
