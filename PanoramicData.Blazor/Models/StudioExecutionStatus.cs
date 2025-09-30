namespace PanoramicData.Blazor.Models;

/// <summary>
/// Execution status values for PDStudio services.
/// </summary>
public enum StudioExecutionStatus
{
    /// <summary>
    /// Studio is ready and waiting for code execution.
    /// </summary>
    Ready,

    /// <summary>
    /// Starting code execution.
    /// </summary>
    Starting,

    /// <summary>
    /// Starting NCalc expression evaluation.
    /// </summary>
    StartingNCalc,

    /// <summary>
    /// Processing/executing code.
    /// </summary>
    Processing,

    /// <summary>
    /// Parsing NCalc expression.
    /// </summary>
    ParsingExpression,

    /// <summary>
    /// Evaluating NCalc expression.
    /// </summary>
    EvaluatingExpression,

    /// <summary>
    /// Generating output content.
    /// </summary>
    GeneratingOutput,

    /// <summary>
    /// Execution completed successfully.
    /// </summary>
    Complete,

    /// <summary>
    /// Execution was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// An error occurred during execution.
    /// </summary>
    Error,

    /// <summary>
    /// Cancelling execution.
    /// </summary>
    Cancelling,

    /// <summary>
    /// Invalid code syntax or structure.
    /// </summary>
    InvalidCode,

    /// <summary>
    /// Execution timed out.
    /// </summary>
    ExecutionTimedOut,

    /// <summary>
    /// Runtime error occurred during execution.
    /// </summary>
    RuntimeError
}