using System.ComponentModel.DataAnnotations;

namespace FluentFunctions.Functions.Configuration;

/// <summary>App settings under <c>Cleanup__*</c>.</summary>
public sealed class CleanupOptions
{
    public const string Section = "Cleanup";

    /// <summary>NCRONTAB expression with seconds, e.g. <c>0 0 2 * * *</c> for 02:00 UTC daily.</summary>
    [Required]
    public string Schedule { get; init; } = string.Empty;

    [Range(1, 3650)]
    public int RetentionDays { get; init; } = 30;

    [Range(1, 10_000)]
    public int BatchSize { get; init; } = 500;
}
