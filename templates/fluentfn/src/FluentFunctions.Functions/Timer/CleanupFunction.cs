using FluentFunctions.Core.Maintenance;
using FluentFunctions.Functions.Adapters;
using FluentFunctions.Functions.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentFunctions.Functions.Timer;

/// <summary>
/// Scheduled clean-up. The handler decides what to remove; this adapter would do the removing.
/// </summary>
public sealed class CleanupFunction(
    IOptionsMonitor<CleanupOptions> options,
    TimeProvider clock,
    ILogger<CleanupFunction> logger
)
{
    [Function("Cleanup")]
    public Task Run([TimerTrigger("%Cleanup:Schedule%")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;
        var plan = CleanupHandlers.Plan(clock.GetUtcNow(), settings.RetentionDays, settings.BatchSize);

        if (plan.IsFailure)
        {
            // Options validation should make this unreachable; fail loudly so the run shows as failed.
            throw new InvalidOperationException($"Clean-up could not be planned: {plan.Errors.Codes()}");
        }

        Log.CleanupPlanned(logger, plan.Value.RemoveOlderThan, plan.Value.BatchSize);

        // Replace with your I/O, e.g. delete expired blobs or rows in batches of plan.Value.BatchSize.
        return Task.CompletedTask;
    }
}
