using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for StudioExecutionStatus enum.
/// </summary>
public static class StudioExecutionStatusExtensions
{
    /// <summary>
    /// Converts the StudioExecutionStatus enum to a user-friendly display string.
    /// </summary>
    /// <param name="status">The execution status.</param>
    /// <returns>A user-friendly status string.</returns>
    public static string ToDisplayString(this StudioExecutionStatus status)
        => status switch
        {
            StudioExecutionStatus.Ready => "Ready",
            StudioExecutionStatus.Starting => "Starting execution...",
            StudioExecutionStatus.StartingNCalc => "Starting NCalc evaluation...",
            StudioExecutionStatus.Processing => "Processing...",
            StudioExecutionStatus.ParsingExpression => "Parsing expression...",
            StudioExecutionStatus.EvaluatingExpression => "Evaluating expression...",
            StudioExecutionStatus.GeneratingOutput => "Generating output...",
            StudioExecutionStatus.Complete => "Complete",
            StudioExecutionStatus.Cancelled => "Cancelled",
            StudioExecutionStatus.Error => "Error",
            StudioExecutionStatus.Cancelling => "Cancelling...",
            StudioExecutionStatus.InvalidCode => "Invalid code",
            StudioExecutionStatus.ExecutionTimedOut => "Execution timed out",
            StudioExecutionStatus.RuntimeError => "Runtime error",
            _ => status.ToString()
        };
}