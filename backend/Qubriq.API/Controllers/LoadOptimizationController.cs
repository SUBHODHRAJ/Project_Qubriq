using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs.LoadOptimization;
using Qubriq.API.Models;
using Qubriq.API.Services.LoadOptimization;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/load-optimization")]
[Authorize(Roles = "Admin,OperationsDispatcher")]
public sealed class LoadOptimizationController : ControllerBase
{
    private const string RequiredPermission = "fleet.load_optimization.run";

    private readonly QubriqDbContext _db;
    private readonly ILoadOptimizationService _optimizationService;

    public LoadOptimizationController(
        QubriqDbContext db,
        ILoadOptimizationService optimizationService)
    {
        _db = db;
        _optimizationService = optimizationService;
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

        var activeUser = await _db.Users
            .AnyAsync(x => x.Id == id.Value && x.IsActive);

        if (!activeUser)
            return false;

        return await _db.UserRoles
            .Where(x => x.UserId == id.Value)
            .Join(
                _db.RolePermissions,
                userRole => userRole.RoleId,
                rolePermission => rolePermission.RoleId,
                (_, rolePermission) => rolePermission)
            .AnyAsync(x =>
                x.IsAllowed &&
                x.Permission.Code == RequiredPermission);
    }

    private async Task<IActionResult?> CheckPermission()
    {
        return await Allowed() ? null : Forbid();
    }

    // Existing manual optimization endpoint.
    [HttpPost("optimize")]
    public async Task<IActionResult> Optimize(
        [FromBody] LoadOptimizationRequest request)
    {
        var denied = await CheckPermission();
        if (denied != null)
            return denied;

        try
        {
            return Ok(_optimizationService.Optimize(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Lists shipments that have a currently active vehicle assignment.
    [HttpGet("assigned-shipments")]
    public async Task<IActionResult> GetAssignedShipments()
    {
        var denied = await CheckPermission();
        if (denied != null)
            return denied;

        var assignments = await _db.ShipmentAssignments
            .AsNoTracking()
            .Where(x =>
                x.Status == "Active" &&
                x.ReleasedAtUtc == null)
            .Include(x => x.Shipment)
                .ThenInclude(x => x.Packages)
            .Include(x => x.Vehicle)
            .OrderByDescending(x => x.AssignedAtUtc)
            .ToListAsync();

        var result = assignments.Select(x => new
        {
            shipmentId = x.ShipmentId,
            trackingNumber = x.Shipment.TrackingNumber,
            origin = x.Shipment.Origin,
            destination = x.Shipment.Destination,
            shipmentStatus = x.Shipment.Status,

            vehicleId = x.VehicleId,
            registrationNumber = x.Vehicle.RegistrationNumber,
            vehicleType = x.Vehicle.VehicleType,
            capacityKg = x.Vehicle.CapacityKg,
            lengthCm = x.Vehicle.CargoLengthCm,
            widthCm = x.Vehicle.CargoWidthCm,
            heightCm = x.Vehicle.CargoHeightCm,

            packages = x.Shipment.Packages.Select(p => new
            {
                id = p.Id,
                description = p.Description,
                packageCode = p.PackageCode,
                category = p.Category,
                quantity = p.Quantity,
                lengthCm = p.LengthCm,
                widthCm = p.WidthCm,
                heightCm = p.HeightCm,
                weightKg = p.WeightKg,
                isFragile = p.IsFragile
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    // Runs the optimizer against actual shipment package records.
    [HttpPost("shipments/{shipmentId:long}/vehicles/{vehicleId:long}/optimize")]
    public async Task<IActionResult> OptimizeShipment(
        long shipmentId,
        long vehicleId)
    {
        var denied = await CheckPermission();
        if (denied != null)
            return denied;

        var userId = UserId();
        if (!userId.HasValue)
            return Forbid();

        var assignment = await _db.ShipmentAssignments
            .AsNoTracking()
            .Include(x => x.Shipment)
                .ThenInclude(x => x.Packages)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x =>
                x.ShipmentId == shipmentId &&
                x.VehicleId == vehicleId &&
                x.Status == "Active" &&
                x.ReleasedAtUtc == null);

        if (assignment == null)
        {
            return BadRequest(new
            {
                message = "No active assignment exists between this shipment and vehicle."
            });
        }

        var shipment = assignment.Shipment;
        var vehicle = assignment.Vehicle;

        if (vehicle.CargoLengthCm is null ||
            vehicle.CargoWidthCm is null ||
            vehicle.CargoHeightCm is null ||
            vehicle.CargoLengthCm <= 0 ||
            vehicle.CargoWidthCm <= 0 ||
            vehicle.CargoHeightCm <= 0 ||
            vehicle.CapacityKg <= 0)
        {
            return BadRequest(new
            {
                message = "The assigned vehicle is missing valid cargo dimensions or payload capacity. Update the vehicle in Fleet Management."
            });
        }

        var packageGroups = shipment.Packages
            .OrderBy(x => x.Id)
            .ToList();

        if (packageGroups.Count == 0)
        {
            return BadRequest(new
            {
                message = "This shipment has no registered package records. Add its package groups before running 3D optimization."
            });
        }

        if (packageGroups.Any(x =>
            x.Quantity < 1 ||
            x.LengthCm <= 0 ||
            x.WidthCm <= 0 ||
            x.HeightCm <= 0 ||
            x.WeightKg <= 0))
        {
            return BadRequest(new
            {
                message = "One or more shipment package records have invalid quantity, dimensions or weight."
            });
        }

        var request = new LoadOptimizationRequest
        {
            VehicleLengthCm = vehicle.CargoLengthCm.Value,
            VehicleWidthCm = vehicle.CargoWidthCm.Value,
            VehicleHeightCm = vehicle.CargoHeightCm.Value,
            MaxPayloadKg = vehicle.CapacityKg,

            Packages = packageGroups.Select(x => new PackageInputDto
            {
                PackageId = x.Id.ToString(),
                Description = x.Description,
                LengthCm = x.LengthCm,
                WidthCm = x.WidthCm,
                HeightCm = x.HeightCm,
                WeightKg = x.WeightKg,
                Quantity = x.Quantity,
                IsFragile = x.IsFragile
            }).ToList()
        };

        LoadOptimizationResponse result;

        try
        {
            result = _optimizationService.Optimize(request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var packageLookup = packageGroups.ToDictionary(x => x.Id);

        var run = new OptimizationRun
        {
            ShipmentId = shipmentId,
            VehicleId = vehicleId,
            CreatedByUserId = userId.Value,
            Algorithm = "ExtremePoint3D-Heuristic",
            Status = result.Status,
            IsFeasible = result.IsFeasible,

            VehicleLengthCm = vehicle.CargoLengthCm.Value,
            VehicleWidthCm = vehicle.CargoWidthCm.Value,
            VehicleHeightCm = vehicle.CargoHeightCm.Value,
            VehicleCapacityKg = vehicle.CapacityKg,

            TotalPackages = result.TotalPackages,
            PlacedPackages = result.PlacedPackages,
            UnplacedPackages = result.UnplacedPackages,

            TotalWeightKg = result.TotalCargoWeightKg,
            UsedWeightKg = result.PlacedCargoWeightKg,
            WeightUtilizationPercent = result.PayloadUtilizationPercent,
            VolumeUtilizationPercent = result.VolumeUtilizationPercent,

            Warnings = JsonSerializer.Serialize(result.Warnings),
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var item in result.Placements)
        {
            if (!long.TryParse(item.PackageId, out var packageId) ||
                !packageLookup.TryGetValue(packageId, out var sourcePackage))
            {
                return BadRequest(new
                {
                    message = "The optimizer returned a package ID that does not belong to this shipment."
                });
            }

            run.Placements.Add(new PackagePlacement
            {
                ShipmentPackageId = sourcePackage.Id,
                UnitNumber = item.UnitNumber,
                PackageDescription = item.Description ?? sourcePackage.Description,

                LengthCm = item.LengthCm,
                WidthCm = item.WidthCm,
                HeightCm = item.HeightCm,
                WeightKg = item.WeightKg,
                IsFragile = item.IsFragile,

                PlacementStatus = "Placed",
                X = item.X,
                Y = item.Y,
                Z = item.Z,
                Orientation = item.Orientation
            });
        }

        foreach (var item in result.UnplacedItems)
        {
            if (!long.TryParse(item.PackageId, out var packageId) ||
                !packageLookup.TryGetValue(packageId, out var sourcePackage))
            {
                return BadRequest(new
                {
                    message = "The optimizer returned an unplaced package ID that does not belong to this shipment."
                });
            }

            run.Placements.Add(new PackagePlacement
            {
                ShipmentPackageId = sourcePackage.Id,
                UnitNumber = item.UnitNumber,
                PackageDescription = item.Description ?? sourcePackage.Description,

                LengthCm = sourcePackage.LengthCm,
                WidthCm = sourcePackage.WidthCm,
                HeightCm = sourcePackage.HeightCm,
                WeightKg = item.WeightKg,
                IsFragile = sourcePackage.IsFragile,

                PlacementStatus = "Unplaced",
                X = null,
                Y = null,
                Z = null,
                Orientation = null,
                UnplacedReason = item.Reason
            });
        }

        // Save the run and all unit placements atomically.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            _db.OptimizationRuns.Add(run);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Ok(new
        {
            optimizationRunId = run.Id,
            shipmentId = run.ShipmentId,
            vehicleId = run.VehicleId,
            createdAtUtc = run.CreatedAtUtc,
            result
        });
    }

    [HttpGet("shipments/{shipmentId:long}/runs")]
    public async Task<IActionResult> GetShipmentRuns(long shipmentId)
    {
        var denied = await CheckPermission();
        if (denied != null)
            return denied;

        var exists = await _db.Shipments
            .AnyAsync(x => x.Id == shipmentId);

        if (!exists)
            return NotFound(new { message = "Shipment not found." });

        var runs = await _db.OptimizationRuns
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipmentId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                id = x.Id,
                shipmentId = x.ShipmentId,
                vehicleId = x.VehicleId,
                vehicleRegistration = x.Vehicle.RegistrationNumber,
                algorithm = x.Algorithm,
                status = x.Status,
                isFeasible = x.IsFeasible,
                approvalStatus = x.Approvals
                    .OrderByDescending(a => a.ReviewedAtUtc)
                    .Select(a => a.Decision)
                    .FirstOrDefault() ?? "Pending",
                totalPackages = x.TotalPackages,
                placedPackages = x.PlacedPackages,
                unplacedPackages = x.UnplacedPackages,
                totalWeightKg = x.TotalWeightKg,
                usedWeightKg = x.UsedWeightKg,
                volumeUtilizationPercent = x.VolumeUtilizationPercent,
                weightUtilizationPercent = x.WeightUtilizationPercent,
                createdAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(runs);
    }

    // Reconstructs the same response shape consumed by LoadScene3D.
    [HttpGet("runs/{runId:long}")]
    public async Task<IActionResult> GetRun(long runId)
    {
        var denied = await CheckPermission();
        if (denied != null)
            return denied;

        var run = await _db.OptimizationRuns
            .AsNoTracking()
            .Include(x => x.Placements)
            .FirstOrDefaultAsync(x => x.Id == runId);

        if (run == null)
            return NotFound(new { message = "Optimization run not found." });

        var placed = run.Placements
            .Where(x => x.PlacementStatus == "Placed")
            .Select(x => new PackagePlacementDto
            {
                PackageId = x.ShipmentPackageId.ToString(),
                Description = x.PackageDescription,
                UnitNumber = x.UnitNumber,
                X = x.X ?? 0,
                Y = x.Y ?? 0,
                Z = x.Z ?? 0,
                LengthCm = x.LengthCm,
                WidthCm = x.WidthCm,
                HeightCm = x.HeightCm,
                WeightKg = x.WeightKg,
                IsFragile = x.IsFragile,
                Orientation = x.Orientation ?? ""
            })
            .ToList();

        var unplaced = run.Placements
            .Where(x => x.PlacementStatus == "Unplaced")
            .Select(x => new UnplacedPackageDto
            {
                PackageId = x.ShipmentPackageId.ToString(),
                Description = x.PackageDescription,
                UnitNumber = x.UnitNumber,
                WeightKg = x.WeightKg,
                Reason = x.UnplacedReason ?? "No valid placement."
            })
            .ToList();

        List<string> warnings;

        try
        {
            warnings = string.IsNullOrWhiteSpace(run.Warnings)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(run.Warnings)
                    ?? new List<string>();
        }
        catch (JsonException)
        {
            warnings = new List<string>
            {
                "Warnings could not be read from this saved optimization run."
            };
        }

        var response = new LoadOptimizationResponse
        {
            IsFeasible = run.IsFeasible,
            Status = run.Status,

            Vehicle = new VehicleSpaceDto
            {
                LengthCm = run.VehicleLengthCm,
                WidthCm = run.VehicleWidthCm,
                HeightCm = run.VehicleHeightCm,
                MaxPayloadKg = run.VehicleCapacityKg,
                UsedVolumeCm3 =
                    run.VehicleLengthCm *
                    run.VehicleWidthCm *
                    run.VehicleHeightCm *
                    run.VolumeUtilizationPercent / 100m,
                TotalVolumeCm3 =
                    run.VehicleLengthCm *
                    run.VehicleWidthCm *
                    run.VehicleHeightCm
            },

            TotalPackages = run.TotalPackages,
            PlacedPackages = run.PlacedPackages,
            UnplacedPackages = run.UnplacedPackages,

            TotalCargoWeightKg = run.TotalWeightKg,
            PlacedCargoWeightKg = run.UsedWeightKg,
            RemainingPayloadKg = run.VehicleCapacityKg - run.UsedWeightKg,

            VolumeUtilizationPercent = run.VolumeUtilizationPercent,
            PayloadUtilizationPercent = run.WeightUtilizationPercent,

            Placements = placed,
            UnplacedItems = unplaced,
            Warnings = warnings
        };

        return Ok(new
        {
            optimizationRunId = run.Id,
            shipmentId = run.ShipmentId,
            vehicleId = run.VehicleId,
            createdAtUtc = run.CreatedAtUtc,
            result = response
        });
    }
}

