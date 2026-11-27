
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/shipments/{shipmentId:long}/packages")]
[Authorize(Roles = "Admin,OperationsDispatcher")]
public class ShipmentPackagesController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public ShipmentPackagesController(QubriqDbContext db)
    {
        _db = db;
    }

    private long? UserId()
    {
        var value = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(value, out var id) ? id : null;
    }

    private async Task<bool> Allowed()
    {
        var id = UserId();

        if (!id.HasValue)
            return false;

        if (!await _db.Users.AnyAsync(
            x => x.Id == id.Value && x.IsActive))
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
                x.Permission.Code == "cargo.manage");
    }

    public sealed class PackageRequest
    {
        public string Description { get; set; } = "";
        public string? PackageCode { get; set; }
        public string? Category { get; set; }
        public int Quantity { get; set; }
        public decimal LengthCm { get; set; }
        public decimal WidthCm { get; set; }
        public decimal HeightCm { get; set; }
        public decimal WeightKg { get; set; }
        public bool IsFragile { get; set; }
        public string? SpecialHandlingRequirements { get; set; }
    }

    private static object ToResponse(ShipmentPackage p)
    {
        return new
        {
            p.Id,
            p.ShipmentId,
            p.Description,
            p.PackageCode,
            p.Category,
            p.Quantity,
            p.LengthCm,
            p.WidthCm,
            p.HeightCm,
            p.WeightKg,
            TotalWeightKg = p.WeightKg * p.Quantity,
            VolumeCm3 = p.LengthCm * p.WidthCm * p.HeightCm,
            p.IsFragile,
            p.SpecialHandlingRequirements,
            p.CreatedAtUtc
        };
    }

    private async Task<bool> ShipmentExists(long shipmentId)
    {
        return await _db.Shipments.AnyAsync(x => x.Id == shipmentId);
    }

    private async Task<bool> IsLocked(long shipmentId)
    {
        // Cargo can be maintained after vehicle assignment,
        // but remains locked once an optimization run exists.
        return await _db.OptimizationRuns.AnyAsync(r =>
            r.ShipmentId == shipmentId);
    }

    private async Task UpdateShipmentSummary(long shipmentId)
    {
        var shipment = await _db.Shipments
            .FirstAsync(x => x.Id == shipmentId);

        var packages = await _db.ShipmentPackages
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync();

        shipment.PackageCount = packages.Sum(x => x.Quantity);
        shipment.TotalWeightKg = packages.Sum(
            x => x.WeightKg * x.Quantity);

        await _db.SaveChangesAsync();
    }

    private IActionResult Validate(PackageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description) ||
            request.Description.Trim().Length > 250)
            return BadRequest(new { message = "Package description is required (maximum 250 characters)." });

        if (request.Quantity < 1)
            return BadRequest(new { message = "Quantity must be at least 1." });

        if (request.LengthCm <= 0 ||
            request.WidthCm <= 0 ||
            request.HeightCm <= 0)
            return BadRequest(new { message = "All package dimensions must be greater than zero." });

        if (request.WeightKg <= 0)
            return BadRequest(new { message = "Unit weight must be greater than zero." });

        if (request.PackageCode?.Length > 80 ||
            request.Category?.Length > 100 ||
            request.SpecialHandlingRequirements?.Length > 500)
            return BadRequest(new { message = "One or more package fields exceed the allowed length." });

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetPackages(long shipmentId)
    {
        if (!await Allowed()) return Forbid();
        if (!await ShipmentExists(shipmentId)) return NotFound();

        var packages = await _db.ShipmentPackages
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return Ok(packages.Select(ToResponse));
    }

    [HttpPost]
    public async Task<IActionResult> CreatePackage(
        long shipmentId,
        [FromBody] PackageRequest request)
    {
        if (!await Allowed()) return Forbid();
        if (!await ShipmentExists(shipmentId)) return NotFound();

        var validation = Validate(request);
        if (validation is BadRequestObjectResult) return validation;

        if (await IsLocked(shipmentId))
            return Conflict(new { message = "Packages cannot be changed after an active assignment or optimization run." });

        var package = new ShipmentPackage
        {
            ShipmentId = shipmentId,
            Description = request.Description.Trim(),
            PackageCode = request.PackageCode?.Trim(),
            Category = request.Category?.Trim(),
            Quantity = request.Quantity,
            LengthCm = request.LengthCm,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            IsFragile = request.IsFragile,
            SpecialHandlingRequirements = request.SpecialHandlingRequirements?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ShipmentPackages.Add(package);
        await _db.SaveChangesAsync();
        await UpdateShipmentSummary(shipmentId);

        return Created(
            $"/api/shipments/{shipmentId}/packages/{package.Id}",
            ToResponse(package));
    }

    [HttpPut("{packageId:long}")]
    public async Task<IActionResult> UpdatePackage(
        long shipmentId,
        long packageId,
        [FromBody] PackageRequest request)
    {
        if (!await Allowed()) return Forbid();
        if (!await ShipmentExists(shipmentId)) return NotFound();

        var package = await _db.ShipmentPackages.FirstOrDefaultAsync(
            x => x.Id == packageId && x.ShipmentId == shipmentId);

        if (package == null) return NotFound();

        var validation = Validate(request);
        if (validation is BadRequestObjectResult) return validation;

        if (await IsLocked(shipmentId))
            return Conflict(new { message = "Packages cannot be changed after an active assignment or optimization run." });

        package.Description = request.Description.Trim();
        package.PackageCode = request.PackageCode?.Trim();
        package.Category = request.Category?.Trim();
        package.Quantity = request.Quantity;
        package.LengthCm = request.LengthCm;
        package.WidthCm = request.WidthCm;
        package.HeightCm = request.HeightCm;
        package.WeightKg = request.WeightKg;
        package.IsFragile = request.IsFragile;
        package.SpecialHandlingRequirements =
            request.SpecialHandlingRequirements?.Trim();

        await _db.SaveChangesAsync();
        await UpdateShipmentSummary(shipmentId);

        return Ok(ToResponse(package));
    }

    [HttpDelete("{packageId:long}")]
    public async Task<IActionResult> DeletePackage(
        long shipmentId,
        long packageId)
    {
        if (!await Allowed()) return Forbid();
        if (!await ShipmentExists(shipmentId)) return NotFound();

        var package = await _db.ShipmentPackages.FirstOrDefaultAsync(
            x => x.Id == packageId && x.ShipmentId == shipmentId);

        if (package == null) return NotFound();

        if (await IsLocked(shipmentId))
            return Conflict(new { message = "Packages cannot be deleted after an active assignment or optimization run." });

        if (await _db.PackagePlacements.AnyAsync(
            x => x.ShipmentPackageId == packageId))
            return Conflict(new { message = "This package has placement history and cannot be deleted." });

        _db.ShipmentPackages.Remove(package);
        await _db.SaveChangesAsync();
        await UpdateShipmentSummary(shipmentId);

        return NoContent();
    }
}


