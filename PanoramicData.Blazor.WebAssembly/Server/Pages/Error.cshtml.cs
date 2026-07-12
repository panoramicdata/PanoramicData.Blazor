namespace PanoramicData.Blazor.WebAssembly.Server.Pages;

/// <summary>
/// Page model for the default error page, displaying the current request identifier.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
	/// <summary>Gets or sets the current HTTP request identifier, used for correlation when reporting errors.</summary>
	public string RequestId { get; set; } = string.Empty;

	/// <summary>Gets a value indicating whether <see cref="RequestId"/> should be displayed.</summary>
	public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

	/// <summary>Handles HTTP GET requests by capturing the current request identifier.</summary>
	public void OnGet() => RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
}
