using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// The IPDStudioService interface defines required properties, methods and events
/// for code execution services in PDStudio components.
/// </summary>
public interface IPDStudioService
{
	/// <summary>
	/// Gets the supported languages that this service can execute.
	/// </summary>
	IEnumerable<string> GetSupportedLanguages();

	/// <summary>
	/// Gets the default language for this service.
	/// </summary>
	string GetDefaultLanguage();

	/// <summary>
	/// Determines if the specified language is supported by this service.
	/// </summary>
	/// <param name="language">The language to check.</param>
	/// <returns>True if supported, false otherwise.</returns>
	bool IsLanguageSupported(string language);

	/// <summary>
	/// Executes the provided code in the specified language.
	/// </summary>
	/// <param name="code">The code to execute.</param>
	/// <param name="language">The programming language.</param>
	/// <param name="cancellationToken">Cancellation token to stop execution.</param>
	/// <returns>The execution result as HTML content.</returns>
	Task<string> ExecuteCodeAsync(
		string code,
		string language,
		CancellationToken cancellationToken);

	/// <summary>
	/// Executes the provided code in the specified language with progress updates.
	/// </summary>
	/// <param name="code">The code to execute.</param>
	/// <param name="language">The programming language.</param>
	/// <param name="resultsProgress">Progress reporter for streaming results updates.</param>
	/// <param name="cancellationToken">Cancellation token to stop execution.</param>
	/// <returns>The final execution result as HTML content.</returns>
	Task<string> ExecuteCodeAsync(
		string code,
		string language,
		IProgress<string>? resultsProgress,
		CancellationToken cancellationToken);

	/// <summary>
	/// Executes the provided code in the specified language with timeout and progress updates.
	/// </summary>
	/// <param name="code">The code to execute.</param>
	/// <param name="language">The programming language.</param>
	/// <param name="resultsProgress">Progress reporter for streaming results updates.</param>
	/// <param name="timeoutSeconds">Timeout in seconds. Use 0 or negative to disable timeout.</param>
	/// <param name="cancellationToken">Cancellation token to stop execution.</param>
	/// <returns>The final execution result as HTML content.</returns>
	Task<string> ExecuteCodeAsync(
		string code,
		string language,
		IProgress<string>? resultsProgress,
		int timeoutSeconds,
		CancellationToken cancellationToken);

	/// <summary>
	/// Event raised during code execution to provide status updates, logs, and results.
	/// </summary>
	event EventHandler<StudioExecutionEventArgs>? ExecutionEvent;

	/// <summary>
	/// Gets whether the service is currently executing code.
	/// </summary>
	bool IsExecuting { get; }

	/// <summary>
	/// Gets the current execution status.
	/// </summary>
	StudioExecutionStatus CurrentStatus { get; }
}