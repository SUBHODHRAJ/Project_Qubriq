using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/fleet")]
[Authorize(Roles = "Admin,OperationsDispatcher")]
public class FleetController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public FleetController(QubriqDbContext db)
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

    // PLANNING COMPATIBILITY

    private static bool FitsInsideVehicle(
        decimal length,
        decimal width,
        decimal height,
        decimal vehicleLength,
        decimal vehicleWidth,
        decimal vehicleHeight)
    {
        // A package may be rotated into any of its six axis-aligned orientations.
        return
            (length <= vehicleLength && width <= vehicleWidth && height <= vehicleHeight) ||
            (length <= vehicleLength && height <= vehicleWidth && width <= vehicleHeight) ||
            (width <= vehicleLength && length <= vehicleWidth && height <= vehicleHeight) ||
            (width <= vehicleLength && height <= vehicleWidth && length <= vehicleHeight) ||
            (height <= vehicleLength && length <= vehicleWidth && width <= vehicleHeight) ||
            (height <= vehicleLength && width <= vehicleWidth && length <= vehicleHeight);
    }

    // VEHICLES

    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles()
    {
        if (!await Allowed("fleet.vehicles.manage")) return Forbid();

        var items = await _db.Vehicles
            .AsNoTracking()
            .OrderBy(x => x.RegistrationNumber)
            .Select(x => new
            {
                x.Id,
                x.RegistrationNumber,
                x.VehicleType,
                x.CapacityKg,
                x.CargoLengthCm,
                x.CargoWidthCm,
                x.CargoHeightCm,
                Status = _db.ShipmentAssignments.Any(a =>a.VehicleId == x.Id && a.Status == "Active")? "Assigned": x.Status
            }).ToListAsync();

        return Ok(items);
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> CreateVehicle(VehicleRequest request)
    {
        if (!await Allowed("fleet.vehicles.manage")) return Forbid();

        if (string.IsNullOrWhiteSpace(request.RegistrationNumber) ||
            string.IsNullOrWhiteSpace(request.VehicleType) ||
            request.RegistrationNumber.Trim().Length > 30 ||
            request.VehicleType.Trim().Length > 80 ||
            request.CapacityKg <= 0 ||
            !request.CargoLengthCm.HasValue ||
            !request.CargoWidthCm.HasValue ||
            !request.CargoHeightCm.HasValue ||
            request.CargoLengthCm.Value <= 0 ||
            request.CargoWidthCm.Value <= 0 ||
            request.CargoHeightCm.Value <= 0)
            return BadRequest(new { message = "Enter valid vehicle details, positive payload capacity and positive cargo dimensions in centimetres." });

        var registration = request.RegistrationNumber.Trim().ToUpperInvariant();

        if (await _db.Vehicles.AnyAsync(x => x.RegistrationNumber == registration))
            return Conflict(new { message = "Registration number already exists." });

        var vehicle = new Vehicle
        {
            RegistrationNumber = registration,
            VehicleType = request.VehicleType.Trim(),
            CapacityKg = request.CapacityKg,
            CargoLengthCm = request.CargoLengthCm,
            CargoWidthCm = request.CargoWidthCm,
            CargoHeightCm = request.CargoHeightCm,
            Status = "Available"
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVehicles), new { },
            new { vehicle.Id, vehicle.RegistrationNumber, vehicle.Status });
    }

    [HttpPut("vehicles/{id:long}")]
    public async Task<IActionResult> UpdateVehicle(long id, VehicleRequest request)
    {
        if (!await Allowed("fleet.vehicles.manage")) return Forbid();

        if (string.IsNullOrWhiteSpace(request.RegistrationNumber) ||
            string.IsNullOrWhiteSpace(request.VehicleType) ||
            request.RegistrationNumber.Trim().Length > 30 ||
            request.VehicleType.Trim().Length > 80 ||
            request.CapacityKg <= 0 ||
            !request.CargoLengthCm.HasValue ||
            !request.CargoWidthCm.HasValue ||
            !request.CargoHeightCm.HasValue ||
            request.CargoLengthCm.Value <= 0 ||
            request.CargoWidthCm.Value <= 0 ||
            request.CargoHeightCm.Value <= 0)
        {
            return BadRequest(new { message = "Enter valid vehicle details, positive payload capacity and positive cargo dimensions in centimetres." });
        }

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (vehicle == null) return NotFound();

        var registration = request.RegistrationNumber.Trim().ToUpperInvariant();

        if (await _db.Vehicles.AnyAsync(x =>
            x.Id != id && x.RegistrationNumber == registration))
        {
            return Conflict(new { message = "Registration number already exists." });
        }

        vehicle.RegistrationNumber = registration;
        vehicle.VehicleType = request.VehicleType.Trim();
        vehicle.CapacityKg = request.CapacityKg;
        vehicle.CargoLengthCm = request.CargoLengthCm.Value;
        vehicle.CargoWidthCm = request.CargoWidthCm.Value;
        vehicle.CargoHeightCm = request.CargoHeightCm.Value;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            vehicle.Id,
            vehicle.RegistrationNumber,
            vehicle.VehicleType,
            vehicle.CapacityKg,
            vehicle.CargoLengthCm,
            vehicle.CargoWidthCm,
            vehicle.CargoHeightCm,
            vehicle.Status
        });
    }

    [HttpPatch("vehicles/{id:long}/status")]
    public async Task<IActionResult> UpdateVehicleStatus(long id, StatusRequest request)
    {
        if (!await Allowed("fleet.vehicles.manage")) return Forbid();

        if (request.Status is not ("Available" or "Maintenance" or "Inactive"))
            return BadRequest(new { message = "Status must be Available, Maintenance or Inactive." });

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == id);
        if (vehicle == null) return NotFound();

        var active = await _db.ShipmentAssignments.AnyAsync(
            x => x.VehicleId == id && x.Status == "Active");

        if (active && request.Status != "Available")
            return Conflict(new { message = "Release the active assignment before changing vehicle status." });

        vehicle.Status = request.Status;
        await _db.SaveChangesAsync();
        return Ok(new { vehicle.Id, vehicle.Status });
    }

    // DRIVERS

    [HttpGet("drivers")]
    public async Task<IActionResult> GetDrivers()
    {
        if (!await Allowed("fleet.drivers.manage")) return Forbid();

        var items = await _db.Drivers
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Phone,
                x.LicenseNumber,
                Status = _db.ShipmentAssignments.Any(a =>a.DriverId == x.Id && a.Status == "Active") ? "Assigned": x.Status,
                x.UserId
            }).ToListAsync();

        return Ok(items);
    }

    [HttpPost("drivers")]
    public async Task<IActionResult> CreateDriver(DriverRequest request)
    {
        if (!await Allowed("fleet.drivers.manage")) return Forbid();

        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.LicenseNumber) ||
            request.FullName.Trim().Length > 150 ||
            request.LicenseNumber.Trim().Length > 60 ||
            (request.Phone?.Length ?? 0) > 30)
            return BadRequest(new { message = "Enter valid driver name, license number and phone." });

        var license = request.LicenseNumber.Trim().ToUpperInvariant();

        if (await _db.Drivers.AnyAsync(x => x.LicenseNumber == license))
            return Conflict(new { message = "License number already exists." });

        var driver = new Driver
        {
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            LicenseNumber = license,
            Status = "Available"
        };

        _db.Drivers.Add(driver);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDrivers), new { },
            new { driver.Id, driver.FullName, driver.Status });
    }

    [HttpPatch("drivers/{id:long}/status")]
    public async Task<IActionResult> UpdateDriverStatus(long id, StatusRequest request)
    {
        if (!await Allowed("fleet.drivers.manage")) return Forbid();

        if (request.Status is not ("Available" or "OnLeave" or "Inactive"))
            return BadRequest(new { message = "Status must be Available, OnLeave or Inactive." });

        var driver = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == id);
        if (driver == null) return NotFound();

        var active = await _db.ShipmentAssignments.AnyAsync(
            x => x.DriverId == id && x.Status == "Active");

        if (active && request.Status != "Available")
            return Conflict(new { message = "Release the active assignment before changing driver status." });

        driver.Status = request.Status;
        await _db.SaveChangesAsync();
        return Ok(new { driver.Id, driver.Status });
    }

    // ASSIGNMENTS

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments()
    {
        if (!await Allowed("fleet.assignments.manage")) return Forbid();

        var items = await _db.ShipmentAssignments
            .AsNoTracking()
            .Include(x => x.Shipment)
            .Include(x => x.Driver)
            .Include(x => x.Vehicle)
            .OrderByDescending(x => x.AssignedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ShipmentId,
                trackingNumber = x.Shipment.TrackingNumber,
                driver = x.Driver.FullName,
                vehicle = x.Vehicle.RegistrationNumber,
                x.AssignedAtUtc,
                x.ReleasedAtUtc,
                x.Status
            }).ToListAsync();

        return Ok(items);
    }

    [HttpPost("assignments/{id:long}/release")]
    public async Task<IActionResult> ReleaseAssignment(long id)
    {
        if (!await Allowed("fleet.assignments.manage"))
            return Forbid();

        await using var tx = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);

        var assignment = await _db.ShipmentAssignments
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (assignment == null)
            return NotFound(new { message = "Assignment not found." });

        if (assignment.Status != "Active" ||
            assignment.ReleasedAtUtc != null)
            return Conflict(new { message = "Assignment is already closed." });

        if (assignment.Shipment.Status != "Assigned")
            return Conflict(new
            {
                message = "Only shipments that have not been picked up can be manually released."
            });

        var releasedAt = DateTime.UtcNow;

        assignment.Status = "Released";
        assignment.ReleasedAtUtc = releasedAt;
        assignment.Shipment.Status = "Confirmed";

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = UserId(),
            Action = "SHIPMENT_ASSIGNMENT_RELEASED",
            EntityType = "Shipment",
            EntityId = assignment.ShipmentId.ToString(),
            Details = $"AssignmentId={assignment.Id}; DriverId={assignment.DriverId}; VehicleId={assignment.VehicleId}",
            CreatedAtUtc = releasedAt
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            assignment.Id,
            assignment.ShipmentId,
            assignment.DriverId,
            assignment.VehicleId,
            assignment.Status,
            assignment.ReleasedAtUtc,
            shipmentStatus = assignment.Shipment.Status,
            message = "Assignment released successfully."
        });
    }
    [HttpPost("assignments")]
    public async Task<IActionResult> Assign(AssignmentRequest request)
    {
        if (!await Allowed("fleet.assignments.manage")) return Forbid();

        if (request.ShipmentId <= 0 || request.DriverId <= 0 || request.VehicleId <= 0)
            return BadRequest(new { message = "Select a shipment, driver and vehicle." });

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var shipment = await _db.Shipments
            .Include(x => x.Packages)
            .FirstOrDefaultAsync(x => x.Id == request.ShipmentId);

        if (shipment == null) return NotFound(new { message = "Shipment not found." });

        if (shipment.Status != "Confirmed")
            return Conflict(new { message = "Only Confirmed bookings can be assigned." });

        var driver = await _db.Drivers.FirstOrDefaultAsync(x => x.Id == request.DriverId);
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == request.VehicleId);

        if (driver == null || vehicle == null)
            return BadRequest(new { message = "Driver or vehicle not found." });

        if (driver.Status != "Available" || vehicle.Status != "Available")
            return Conflict(new { message = "Driver and vehicle must both be Available." });

        if (!vehicle.CargoLengthCm.HasValue ||
            !vehicle.CargoWidthCm.HasValue ||
            !vehicle.CargoHeightCm.HasValue ||
            vehicle.CargoLengthCm.Value <= 0 ||
            vehicle.CargoWidthCm.Value <= 0 ||
            vehicle.CargoHeightCm.Value <= 0)
        {
            return Conflict(new
            {
                message = "Vehicle cargo-compartment dimensions are not configured. Update the vehicle before assigning shipments."
            });
        }

        var vehicleLength = vehicle.CargoLengthCm.Value;
        var vehicleWidth = vehicle.CargoWidthCm.Value;
        var vehicleHeight = vehicle.CargoHeightCm.Value;

        if (shipment.Packages.Count > 0)
        {
            foreach (var package in shipment.Packages)
            {
                if (package.Quantity <= 0 ||
                    package.LengthCm <= 0 ||
                    package.WidthCm <= 0 ||
                    package.HeightCm <= 0 ||
                    package.WeightKg < 0)
                {
                    return Conflict(new
                    {
                        message = $"Package '{package.Description}' has invalid quantity, dimensions or weight. Correct the package details before assignment."
                    });
                }

                if (!FitsInsideVehicle(
                    package.LengthCm,
                    package.WidthCm,
                    package.HeightCm,
                    vehicleLength,
                    vehicleWidth,
                    vehicleHeight))
                {
                    return Conflict(new
                    {
                        message = $"Package '{package.Description}' cannot fit inside vehicle {vehicle.RegistrationNumber}, even after rotation."
                    });
                }
            }
        }
        else
        {
            if (!shipment.LengthCm.HasValue ||
                !shipment.WidthCm.HasValue ||
                !shipment.HeightCm.HasValue ||
                shipment.LengthCm.Value <= 0 ||
                shipment.WidthCm.Value <= 0 ||
                shipment.HeightCm.Value <= 0)
            {
                return Conflict(new
                {
                    message = "Shipment dimensions are missing. Add valid cargo length, width and height before assignment."
                });
            }

            if (!FitsInsideVehicle(
                shipment.LengthCm.Value,
                shipment.WidthCm.Value,
                shipment.HeightCm.Value,
                vehicleLength,
                vehicleWidth,
                vehicleHeight))
            {
                return Conflict(new
                {
                    message = "Shipment dimensions exceed the selected vehicle's cargo compartment, even after rotation."
                });
            }
        }

        var packageWeight = shipment.Packages.Count > 0
            ? shipment.Packages.Sum(x => x.WeightKg * x.Quantity)
            : (decimal?)null;

        var requiredWeight = shipment.TotalWeightKg ?? packageWeight;

        if (!requiredWeight.HasValue || requiredWeight.Value <= 0)
        {
            return Conflict(new
            {
                message = "Shipment weight is missing. Add a valid total cargo weight before assignment."
            });
        }

        if (requiredWeight.Value > vehicle.CapacityKg)
        {
            return Conflict(new
            {
                message = $"Shipment weight ({requiredWeight.Value} kg) exceeds vehicle payload capacity ({vehicle.CapacityKg} kg)."
            });
        }

        var shipmentAssigned = await _db.ShipmentAssignments.AnyAsync(
            x => x.ShipmentId == request.ShipmentId && x.Status == "Active");

        var driverAssigned = await _db.ShipmentAssignments.AnyAsync(
            x => x.DriverId == request.DriverId && x.Status == "Active");

        var vehicleAssigned = await _db.ShipmentAssignments.AnyAsync(
            x => x.VehicleId == request.VehicleId && x.Status == "Active");

        if (shipmentAssigned || driverAssigned || vehicleAssigned)
            return Conflict(new { message = "Shipment, driver or vehicle already has an active assignment." });

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
            UserId = UserId(),
            Action = "SHIPMENT_DRIVER_ASSIGNED",
            EntityType = "Shipment",
            EntityId = shipment.Id.ToString(),
            Details = $"DriverId={driver.Id}; VehicleId={vehicle.Id}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            assignment.Id,
            assignment.ShipmentId,
            assignment.DriverId,
            assignment.VehicleId,
            shipment.Status
        });
    }
}

public sealed class VehicleRequest
{
    public string RegistrationNumber { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public decimal CapacityKg { get; set; }
    public decimal? CargoLengthCm { get; set; }
    public decimal? CargoWidthCm { get; set; }
    public decimal? CargoHeightCm { get; set; }
}

public sealed class DriverRequest
{
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string LicenseNumber { get; set; } = "";
}

public sealed class AssignmentRequest
{
    public long ShipmentId { get; set; }
    public long DriverId { get; set; }
    public long VehicleId { get; set; }
}

public sealed class StatusRequest
{
    public string Status { get; set; } = "";
}

