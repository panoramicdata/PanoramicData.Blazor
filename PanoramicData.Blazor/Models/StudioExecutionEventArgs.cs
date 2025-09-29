namespace PanoramicData.Blazor.Models;

/// <summary>
/// Event arguments for studio execution events.
/// </summary>
public class StudioExecutionEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the type of execution event.
    /// </summary>
    public StudioExecutionEventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the output content (usually HTML).
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution progress (0.0 to 1.0).
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// Gets or sets the log level for logging events.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets any exception that occurred during execution.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets whether the execution is complete.
    /// </summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// Types of studio execution events.
/// </summary>
public enum StudioExecutionEventType
{
    /// <summary>
    /// Execution has started.
    /// </summary>
    Started,

    /// <summary>
    /// Output content has been updated.
    /// </summary>
    UpdateOutput,

    /// <summary>
    /// Output generation is complete.
    /// </summary>
    OutputComplete,

    /// <summary>
    /// Execution progress has been updated.
    /// </summary>
    Progress,

    /// <summary>
    /// A log message has been generated.
    /// </summary>
    Log,

    /// <summary>
    /// An error has occurred.
    /// </summary>
    Error,

    /// <summary>
    /// Execution was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Execution has completed successfully.
    /// </summary>
    Completed
}

/// <summary>
/// Supported programming languages for PDStudio.
/// </summary>
public enum SupportedLanguage
{
    /// <summary>
    /// PostgreSQL database queries.
    /// </summary>
    PostgreSQL,

    /// <summary>
    /// Microsoft SQL Server queries.
    /// </summary>
    MSSQL,

    /// <summary>
    /// NCalc expression language.
    /// </summary>
    NCalc,

    /// <summary>
    /// JSON data format.
    /// </summary>
    JSON,

    /// <summary>
    /// Plain text.
    /// </summary>
    PlainText,

    /// <summary>
    /// HTML markup.
    /// </summary>
    HTML,

    /// <summary>
    /// JavaScript code.
    /// </summary>
    JavaScript,

    /// <summary>
    /// CSS stylesheets.
    /// </summary>
    CSS,

    /// <summary>
    /// XML markup.
    /// </summary>
    XML
}