using FluentFunctions.Core.Results;

namespace FluentFunctions.Core.Maintenance;

/// <summary>What a scheduled clean-up run should remove.</summary>
public sealed record CleanupPlan(DateTimeOffset RemoveOlderThan, int BatchSize);

public static class CleanupHandlers
{
    /// <summary>
    /// Works out the clean-up window for a run. The adapter does the deleting; this decides what
    /// to delete, which is the part worth testing.
    /// </summary>
    public static Result<CleanupPlan> Plan(DateTimeOffset now, int retentionDays, int batchSize) =>
        retentionDays < 1
            ? Error.Validation("cleanup.retention.invalid", "Retention must be at least one day.")
            : new CleanupPlan(now.UtcDateTime.Date.AddDays(-retentionDays), batchSize);
}
