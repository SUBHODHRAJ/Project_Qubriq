using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/operations/shipments")]
[Authorize(Roles = "Admin,OperationsDispatcher")]
public class ShipmentOperationsController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public ShipmentOperationsController(QubriqDbContext db)
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
            .Join(_db.RolePermissions,
                ur => ur.RoleId,
                rp => rp.RoleId,
                (_, rp) => rp)
            .AnyAsync(x => x.IsAllowed && x.Permission.Code == permission);
    }

    [HttpGet]
    public async Task<IActionResult> GetShipments()
    {
        if (!await Allowed("shipments.read_all"))
            return Forbid();

        var items = await _db.Shipments
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.TrackingNumber,
                customer = x.Customer.CompanyName,
                x.Origin,
                x.Destination,
                x.Status,
                x.CreatedAtUtc,
                x.ExpectedDeliveryUtc,
                x.CargoName,
                x.CargoCategory,
                x.PackageCount,
                x.TotalWeightKg,
                x.IsFragile
            })
            .ToListAsync();

        return Ok(items);
    }
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetShipment(long id)
    {
        if (!await Allowed("shipments.read_all"))
            return Forbid();

        var item = await _db.Shipments
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.TrackingNumber,
                customer = x.Customer.CompanyName,
                x.Origin,
                x.Destination,
                x.Status,
                x.CreatedAtUtc,
                x.PickupDateUtc,
                x.ExpectedDeliveryUtc,
                x.CargoName,
                x.CargoCategory,
                x.PackageCount,
                x.TotalWeightKg,
                x.LengthCm,
                x.WidthCm,
                x.HeightCm,
                x.IsFragile,
                x.SpecialHandlingRequirements,
                x.Priority,
                x.DeliveryTimeWindow,
                x.AdditionalInstructions
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { message = "Shipment not found." });

        return Ok(item);
    }

    [HttpGet("{id:long}/tracking")]
    public async Task<IActionResult> GetTracking(long id)
    {
        if (!await Allowed("shipments.read_all"))
            return Forbid();

        var shipment = await _db.Shipments
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.TrackingNumber,
                x.Origin,
                x.Destination,
                x.Status,
                x.CreatedAtUtc,
                x.ExpectedDeliveryUtc
            })
            .FirstOrDefaultAsync();

        if (shipment == null)
            return NotFound(new { message = "Shipment not found." });

        var entityId = id.ToString();

        var history = await _db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.EntityType == "Shipment" &&
                x.EntityId == entityId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Action,
                x.Details,
                x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(new
        {
            shipment,
            history,
            trackingSource = "Recorded audit events",
            note = "This endpoint reports recorded operational events. It does not provide live GPS coordinates."
        });
    }

    [HttpPut("{id:long}/cargo")]
    public async Task<IActionResult> UpdateCargo(
        long id,
        [FromBody] UpdateCargoRequest request)
    {
        if (!await Allowed("shipments.update"))
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.CargoName) ||
            request.CargoName.Trim().Length > 200)
            return BadRequest(new { message = "Cargo name is required and must not exceed 200 characters." });

        if (request.CargoCategory?.Length > 100 ||
            request.SpecialHandlingRequirements?.Length > 500)
            return BadRequest(new { message = "Cargo category or handling requirements exceed the allowed length." });

        if (request.PackageCount <= 0 ||
            request.TotalWeightKg <= 0)
            return BadRequest(new { message = "Package count and total weight must be positive." });

        if ((request.LengthCm.HasValue && request.LengthCm <= 0) ||
            (request.WidthCm.HasValue && request.WidthCm <= 0) ||
            (request.HeightCm.HasValue && request.HeightCm <= 0))
            return BadRequest(new { message = "Dimensions must be positive when provided." });

        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shipment == null)
            return NotFound(new { message = "Shipment not found." });

        if (shipment.Status is "Delivered" or "Cancelled" or "Rejected")
            return Conflict(new { message = "Cargo cannot be edited after shipment completion or cancellation." });

        var oldCargo = shipment.CargoName;

        shipment.CargoName = request.CargoName.Trim();
        shipment.CargoCategory = request.CargoCategory?.Trim();
        shipment.PackageCount = request.PackageCount;
        shipment.TotalWeightKg = request.TotalWeightKg;
        shipment.LengthCm = request.LengthCm;
        shipment.WidthCm = request.WidthCm;
        shipment.HeightCm = request.HeightCm;
        shipment.IsFragile = request.IsFragile;
        shipment.SpecialHandlingRequirements =
            request.SpecialHandlingRequirements?.Trim();

        _db.AuditLogs.Add(new Qubriq.API.Models.AuditLog
        {
            UserId = UserId(),
            Action = "SHIPMENT_CARGO_UPDATED",
            EntityType = "Shipment",
            EntityId = shipment.Id.ToString(),
            Details = $"PreviousCargo={oldCargo}; UpdatedCargo={shipment.CargoName}; Packages={shipment.PackageCount}; WeightKg={shipment.TotalWeightKg}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            shipment.Id,
            shipment.TrackingNumber,
            shipment.CargoName,
            shipment.CargoCategory,
            shipment.PackageCount,
            shipment.TotalWeightKg,
            shipment.LengthCm,
            shipment.WidthCm,
            shipment.HeightCm,
            shipment.IsFragile,
            shipment.SpecialHandlingRequirements,
            message = "Cargo details updated successfully."
        });
    }
}

public sealed class UpdateCargoRequest
{
    public string CargoName { get; set; } = "";
    public string? CargoCategory { get; set; }
    public int PackageCount { get; set; }
    public decimal TotalWeightKg { get; set; }
    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public bool IsFragile { get; set; }
    public string? SpecialHandlingRequirements { get; set; }
}

