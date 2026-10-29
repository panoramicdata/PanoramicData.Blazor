using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// A studio service that can return tabular results, which <see cref="PDStudio"/> shows in a grid.
/// </summary>
public interface IPDStudioResultSetService : IPDStudioService
{
	/// <summary>
	/// Executes the code and returns its results as a table.
	/// </summary>
	/// <param name="code">The code to execute.</param>
	/// <param name="language">The language.</param>
	/// <param name="timeoutSeconds">Timeout in seconds; 0 or negative for none.</param>
	/// <param name="cancellationToken">Cancels the execution.</param>
	/// <returns>The results, or null when they are not tabular, in which case the studio shows the HTML from
	/// <see cref="IPDStudioService.ExecuteCodeAsync(string, string, IProgress{string}?, int, CancellationToken)"/> instead.</returns>
	Task<PDStudioResultSet?> ExecuteResultSetAsync(string code, string language, int timeoutSeconds, CancellationToken cancellationToken);
}
