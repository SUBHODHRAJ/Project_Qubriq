namespace Qubriq.API.Models;

public class LoadingPlanApproval
{
    public long Id { get; set; }

    public long OptimizationRunId { get; set; }

    public long ReviewedByUserId { get; set; }

    // Approved or Rejected
    public string Decision { get; set; } = string.Empty;

    public string? Comments { get; set; }

    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;

    public OptimizationRun OptimizationRun { get; set; } = null!;

    public User ReviewedByUser { get; set; } = null!;
}
