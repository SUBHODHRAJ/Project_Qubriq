using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Authorize(Roles = "ShipmentRecipient")]
[Route("api/recipient/shipments")]
public class RecipientShipmentsController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public RecipientShipmentsController(QubriqDbContext db)
    {
        _db = db;
    }

    private long? UserId()
    {
        var value = User.FindFirst("sub")?.Value ??
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return long.TryParse(value, out var id) ? id : null;
    }

    private async Task<User?> ActiveRecipient()
    {
        var id = UserId();
        if (!id.HasValue) return null;

        return await _db.Users
            .FirstOrDefaultAsync(x => x.Id == id.Value && x.IsActive);
    }

    private async Task<bool> HasPermission(long id, string code)
    {
        return await _db.UserRoles
            .Where(x => x.UserId == id)
            .Join(_db.RolePermissions,
                x => x.RoleId,
                x => x.RoleId,
                (_, p) => p)
            .AnyAsync(x => x.IsAllowed && x.Permission.Code == code);
    }

    private IQueryable<Shipment> Scoped(User user)
    {
        if (user.RecipientType == "DeliveryPersonnel")
        {
            return _db.Shipments.Where(s =>
                s.Assignments.Any(a =>
                    a.Driver.UserId == user.Id &&
                    a.Status == "Active" &&
                    a.ReleasedAtUtc == null));
        }

        if (user.RecipientType == "FinalRecipient")
        {
            return _db.Shipments.Where(s =>
                _db.ShipmentRecipients.Any(r =>
                    r.ShipmentId == s.Id && r.UserId == user.Id));
        }

        return _db.Shipments.Where(s => false);
    }

    private static object ShipmentSummary(Shipment s) => new
    {
        s.Id,
        s.TrackingNumber,
        s.Origin,
        s.Destination,
        s.Status,
        s.CreatedAtUtc,
        s.ExpectedDeliveryUtc,
        s.CargoName,
        s.CargoCategory,
        s.PackageCount,
        s.TotalWeightKg,
        s.DeliveryTimeWindow
    };

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var user = await ActiveRecipient();
        if (user == null) return Forbid();

        var permission = user.RecipientType == "DeliveryPersonnel"
            ? "delivery.assigned.read"
            : user.RecipientType == "FinalRecipient"
                ? "delivery.incoming.read"
                : "";

        if (permission == "" || !await HasPermission(user.Id, permission))
            return Forbid();

        var items = await Scoped(user)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.TrackingNumber,
                x.Origin,
                x.Destination,
                x.Status,
                x.CreatedAtUtc,
                x.ExpectedDeliveryUtc,
                x.CargoName,
                x.CargoCategory,
                x.PackageCount,
                x.TotalWeightKg,
                x.DeliveryTimeWindow
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetOne(long id)
    {
        var user = await ActiveRecipient();
        if (user == null) return Forbid();

        var permission = user.RecipientType == "DeliveryPersonnel"
            ? "delivery.assigned.read"
            : user.RecipientType == "FinalRecipient"
                ? "delivery.incoming.read"
                : "";

        if (permission == "" || !await HasPermission(user.Id, permission))
            return Forbid();

        var shipment = await Scoped(user)
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
                x.ExpectedDeliveryUtc,
                x.CargoName,
                x.CargoCategory,
                x.PackageCount,
                x.TotalWeightKg,
                x.LengthCm,
                x.WidthCm,
                x.HeightCm,
                x.IsFragile,
                x.Priority,
                x.PickupDateUtc,
                x.DeliveryTimeWindow
            })
            .FirstOrDefaultAsync();

        return shipment == null ? NotFound() : Ok(shipment);
    }

    public sealed class StatusRequest
    {
        public string Status { get; set; } = "";
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] StatusRequest request)
    {
        var user = await ActiveRecipient();
        if (user == null || user.RecipientType != "DeliveryPersonnel")
            return Forbid();

        if (!await HasPermission(user.Id, "delivery.status.update"))
            return Forbid();

        var shipment = await Scoped(user)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shipment == null) return NotFound();

        var allowed = new Dictionary<string, string[]>
        {
            ["Assigned"] = new[] { "PickedUp" },
            ["PickedUp"] = new[] { "InTransit" },
            ["InTransit"] = new[] { "Delivered" }
        };

        if (string.IsNullOrWhiteSpace(request.Status) ||
            !allowed.TryGetValue(shipment.Status, out var next))
            return BadRequest(new { message = "Invalid shipment status transition." });

        var canonicalStatus = next.FirstOrDefault(
            x => string.Equals(x, request.Status.Trim(), StringComparison.OrdinalIgnoreCase));

        if (canonicalStatus == null)
            return BadRequest(new { message = "Invalid shipment status transition." });

        var oldStatus = shipment.Status;
        shipment.Status = canonicalStatus;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "SHIPMENT_STATUS_CHANGED",
            EntityType = "Shipment",
            EntityId = shipment.Id.ToString(),
            Details = $"From={oldStatus}; To={request.Status}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new { shipment.Id, shipment.TrackingNumber, shipment.Status });
    }

    public sealed class ProofRequest
    {
        public string ProofType { get; set; } = "";
        public string? FileReference { get; set; }
        public string? Remarks { get; set; }
    }

    [HttpPost("{id:long}/proof")]
    public async Task<IActionResult> SubmitProof(
        long id,
        [FromBody] ProofRequest request)
    {
        var user = await ActiveRecipient();
        if (user == null || user.RecipientType != "DeliveryPersonnel")
            return Forbid();

        if (!await HasPermission(user.Id, "delivery.proof.submit"))
            return Forbid();

        var shipment = await Scoped(user)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shipment == null) return NotFound();

        if (shipment.Status != "Delivered")
            return BadRequest(new { message = "Proof can only be submitted after Delivered status." });

        if (string.IsNullOrWhiteSpace(request.ProofType))
            return BadRequest(new { message = "Invalid proof type." });

        var type = request.ProofType.Trim();

        if (type.Length > 50)
            return BadRequest(new { message = "Invalid proof type." });

        if (request.FileReference?.Length > 500 ||
            request.Remarks?.Length > 1000)
            return BadRequest(new { message = "Proof metadata exceeds allowed length." });

        var proof = new DeliveryProof
        {
            ShipmentId = shipment.Id,
            SubmittedByUserId = user.Id,
            ProofType = type,
            FileReference = request.FileReference?.Trim(),
            Remarks = request.Remarks?.Trim(),
            SubmittedAtUtc = DateTime.UtcNow
        };

        _db.DeliveryProofs.Add(proof);

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "DELIVERY_PROOF_SUBMITTED",
            EntityType = "Shipment",
            EntityId = shipment.Id.ToString(),
            Details = $"ProofType={type}; ProofId={proof.Id}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new { proof.Id, proof.ShipmentId, proof.ProofType, proof.SubmittedAtUtc });
    }

    [HttpGet("{id:long}/tracking")]
    public async Task<IActionResult> GetTracking(long id)
    {
        var user = await ActiveRecipient();
        if (user == null) return Forbid();

        var permission = user.RecipientType == "DeliveryPersonnel"
            ? "delivery.assigned.read"
            : user.RecipientType == "FinalRecipient"
                ? "tracking.view_own"
                : "";

        if (permission == "" || !await HasPermission(user.Id, permission))
            return Forbid();

        var shipment = await Scoped(user)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.TrackingNumber,
                x.Status,
                x.CreatedAtUtc,
                x.ExpectedDeliveryUtc
            })
            .FirstOrDefaultAsync();

        if (shipment == null) return NotFound();

        var history = await _db.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.EntityType == "Shipment" &&
                x.EntityId == id.ToString() &&
                x.Action == "SHIPMENT_STATUS_CHANGED")
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Action,
                x.Details,
                x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(new { shipment, history });
    }

    [HttpPost("{id:long}/confirm-receipt")]
    public async Task<IActionResult> ConfirmReceipt(long id)
    {
        var user = await ActiveRecipient();
        if (user == null || user.RecipientType != "FinalRecipient")
            return Forbid();

        if (!await HasPermission(user.Id, "delivery.receipt.confirm"))
            return Forbid();

        var shipment = await Scoped(user)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shipment == null) return NotFound();

        if (shipment.Status != "Delivered")
            return BadRequest(new { message = "Only delivered shipments can be confirmed." });

        await using var transaction =
            await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var alreadyConfirmed = await _db.AuditLogs.AnyAsync(x =>
            x.UserId == user.Id &&
            x.Action == "SHIPMENT_RECEIPT_CONFIRMED" &&
            x.EntityType == "Shipment" &&
            x.EntityId == id.ToString());

        if (alreadyConfirmed)
            return Conflict(new { message = "Receipt has already been confirmed." });

        var confirmedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            Action = "SHIPMENT_RECEIPT_CONFIRMED",
            EntityType = "Shipment",
            EntityId = id.ToString(),
            Details = $"ConfirmedByUserId={user.Id}",
            CreatedAtUtc = confirmedAtUtc
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Shipment receipt confirmed.",
            shipmentId = id,
            confirmedAtUtc
        });
    }
}
