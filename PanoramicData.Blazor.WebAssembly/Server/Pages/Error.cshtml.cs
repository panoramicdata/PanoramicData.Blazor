namespace PanoramicData.Blazor.WebAssembly.Server.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel(ILogger<ErrorModel> logger) : PageModel
{
	public string RequestId { get; set; }

	public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

	public void OnGet() => RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
}
