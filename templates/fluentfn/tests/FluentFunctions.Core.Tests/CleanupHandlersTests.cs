using FluentFunctions.Core.Maintenance;

namespace FluentFunctions.Core.Tests;

public class CleanupHandlersTests
{
    [Fact]
    public void Plan_cuts_off_at_midnight_utc_retention_days_ago()
    {
        var now = new DateTimeOffset(2026, 3, 10, 15, 30, 0, TimeSpan.FromHours(2));

        var plan = CleanupHandlers.Plan(now, retentionDays: 7, batchSize: 100);

        Assert.Equal(new DateTimeOffset(2026, 3, 3, 0, 0, 0, TimeSpan.Zero), plan.Value.RemoveOlderThan);
        Assert.Equal(100, plan.Value.BatchSize);
    }

    [Fact]
    public void Plan_rejects_zero_retention() =>
        Assert.Equal(
            "cleanup.retention.invalid",
            Assert.Single(CleanupHandlers.Plan(DateTimeOffset.UnixEpoch, 0, 100).Errors).Code
        );
}
