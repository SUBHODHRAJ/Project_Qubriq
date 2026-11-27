using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/load-optimization/approvals")]
[Authorize(Roles = "Admin,OperationsDispatcher")]
public sealed class LoadingPlanApprovalsController : ControllerBase
{
    private const string RequiredPermission = "fleet.load_optimization.run";
    private readonly QubriqDbContext _db;

    public LoadingPlanApprovalsController(QubriqDbContext db)
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
        if (!id.HasValue) return false;

        var active = await _db.Users.AnyAsync(
            x => x.Id == id.Value && x.IsActive);

        if (!active) return false;

        return await _db.UserRoles
            .Where(x => x.UserId == id.Value)
            .Join(
                _db.RolePermissions,
                ur => ur.RoleId,
                rp => rp.RoleId,
                (_, rp) => rp)
            .AnyAsync(x =>
                x.IsAllowed &&
                x.Permission.Code == RequiredPermission);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        if (!await Allowed()) return Forbid();

        var runs = await _db.OptimizationRuns
            .AsNoTracking()
            .Where(run => !run.Approvals.Any())
            .OrderByDescending(run => run.CreatedAtUtc)
            .Select(run => new
            {
                runId = run.Id,
                shipmentId = run.ShipmentId,
                trackingNumber = run.Shipment.TrackingNumber,
                origin = run.Shipment.Origin,
                destination = run.Shipment.Destination,
                vehicleRegistration = run.Vehicle.RegistrationNumber,
                run.Algorithm,
                run.Status,
                run.IsFeasible,
                run.TotalPackages,
                run.PlacedPackages,
                run.UnplacedPackages,
                run.VolumeUtilizationPercent,
                run.WeightUtilizationPercent,
                run.CreatedAtUtc,
                approvalStatus = "Pending"
            })
            .ToListAsync();

        return Ok(runs);
    }

    [HttpGet("{runId:long}")]
    public async Task<IActionResult> GetHistory(long runId)
    {
        if (!await Allowed()) return Forbid();

        var exists = await _db.OptimizationRuns
            .AnyAsync(x => x.Id == runId);

        if (!exists)
            return NotFound(new { message = "Optimization run not found." });

        var history = await _db.LoadingPlanApprovals
            .AsNoTracking()
            .Where(x => x.OptimizationRunId == runId)
            .OrderByDescending(x => x.ReviewedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.OptimizationRunId,
                x.ReviewedByUserId,
                reviewerName = x.ReviewedByUser.FullName,
                x.Decision,
                x.Comments,
                x.ReviewedAtUtc
            })
            .ToListAsync();

        return Ok(new
        {
            runId,
            approvalStatus = history.Count == 0
                ? "Pending"
                : history[0].Decision,
            history
        });
    }

    [HttpPost("{runId:long}")]
    public async Task<IActionResult> Review(
        long runId,
        [FromBody] LoadingPlanReviewRequest request)
    {
        if (!await Allowed()) return Forbid();

        var reviewerId = UserId();
        if (!reviewerId.HasValue) return Forbid();

        var decision = request.Decision?.Trim();

        if (decision is not ("Approved" or "Rejected"))
            return BadRequest(new
            {
                message = "Decision must be Approved or Rejected."
            });

        if (request.Comments?.Length > 1000)
            return BadRequest(new
            {
                message = "Comments cannot exceed 1000 characters."
            });

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var run = await _db.OptimizationRuns
            .FirstOrDefaultAsync(x => x.Id == runId);

        if (run == null)
            return NotFound(new { message = "Optimization run not found." });

        var alreadyReviewed = await _db.LoadingPlanApprovals
            .AnyAsync(x => x.OptimizationRunId == runId);

        if (alreadyReviewed)
            return Conflict(new
            {
                message = "This loading plan has already been reviewed. Create a new optimization run for another review."
            });

        if (decision == "Approved" && !run.IsFeasible)
            return BadRequest(new
            {
                message = "A partial or infeasible loading plan cannot be approved. Reject it or generate a new optimization run."
            });

        var now = DateTime.UtcNow;

        var approval = new LoadingPlanApproval
        {
            OptimizationRunId = runId,
            ReviewedByUserId = reviewerId.Value,
            Decision = decision,
            Comments = string.IsNullOrWhiteSpace(request.Comments)
                ? null
                : request.Comments.Trim(),
            ReviewedAtUtc = now
        };

        _db.LoadingPlanApprovals.Add(approval);

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = reviewerId.Value,
            Action = decision == "Approved"
                ? "LOADING_PLAN_APPROVED"
                : "LOADING_PLAN_REJECTED",
            EntityType = "OptimizationRun",
            EntityId = runId.ToString(),
            Details = approval.Comments,
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = decision == "Approved"
                ? "Loading plan approved successfully."
                : "Loading plan rejected.",
            approvalId = approval.Id,
            runId,
            decision,
            comments = approval.Comments,
            reviewedAtUtc = approval.ReviewedAtUtc
        });
    }
}

public sealed class LoadingPlanReviewRequest
{
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
}
