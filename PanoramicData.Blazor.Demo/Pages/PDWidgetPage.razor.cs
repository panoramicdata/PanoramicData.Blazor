using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDWidgetPage
{
	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private string _configureMessage = string.Empty;

	private readonly string _htmlContent = """
		<h5>Welcome</h5>
		<p>This widget renders <strong>sanitized HTML</strong> content.</p>
		<ul>
			<li>Safe tags are preserved</li>
			<li>Script tags are removed</li>
			<li>Event handlers are stripped</li>
		</ul>
		""";

	private void OnConfigureClicked()
	{
		_configureMessage = $"Configure clicked at {DateTime.Now:HH:mm:ss}";
		EventManager?.Add(new Event("OnConfigure"));
	}
}
