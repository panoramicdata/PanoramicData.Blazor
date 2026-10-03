using Microsoft.AspNetCore.Components.Routing;

namespace PanoramicData.Blazor.Demo.Shared;

public partial class DemoSourceView : IDisposable
{
	private string ActiveTab { get; set; } = "Demo";

	[Inject] private INavigationCancelService NavigationCancelService { get; set; } = default!;

	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

	[Inject] private NavigationManager NavigationManager { get; set; } = default!;

	/// <summary>
	/// Sets the child content that the drop zone wraps.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Optional documentation content for the Documentation tab.
	/// </summary>
	[Parameter] public RenderFragment? DocumentationContent { get; set; }

	/// <summary>
	/// Event called prior to the user changing tabs.
	/// </summary>
	[Parameter] public EventCallback<CancelEventArgs> BeforeChangeTab { get; set; }

	/// <summary>
	/// Sets the source code pages used in the demo.
	/// </summary>
	[Parameter] public string SourceFiles { get; set; } = string.Empty;

	/// <summary>
	/// When <c>true</c>, removes the default silver border and padding from the container
	/// so the demo content can provide its own layout without an extra visual frame.
	/// </summary>
	[Parameter] public bool NoBorder { get; set; }

	protected override void OnInitialized()
	{
		NavigationManager.LocationChanged += OnLocationChanged;
		ParseTabFromUrl();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await LoadSourceFilesAsync().ConfigureAwait(true);

			// Scroll to anchor if present
			await ScrollToAnchorAsync().ConfigureAwait(true);
		}
	}

	private void ParseTabFromUrl()
	{
		var uri = new Uri(NavigationManager.Uri);
		var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
		var tab = query["tab"];

		if (!string.IsNullOrEmpty(tab))
		{
			ActiveTab = tab switch
			{
				"demo" => "Demo",
				"source" => "Source",
				"docs" or "documentation" when DocumentationContent != null => "Documentation",
				_ => "Demo"
			};
		}
	}

	private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
	{
		ParseTabFromUrl();
		StateHasChanged();
	}

	private async Task OnChangeTab(string tab)
	{
		if (await NavigationCancelService.ProceedAsync(tab).ConfigureAwait(true))
		{
			ActiveTab = tab;
			UpdateUrlWithTab(tab);
		}
	}

	private void UpdateUrlWithTab(string tab)
	{
		var uri = new Uri(NavigationManager.Uri);
		var baseUrl = uri.GetLeftPart(UriPartial.Path);
		var tabParam = tab.ToLowerInvariant() switch
		{
			"demo" => "demo",
			"source" => "source",
			"documentation" => "docs",
			_ => "demo"
		};

		// Preserve the fragment (anchor) if present
		var fragment = uri.Fragment;
		var newUrl = $"{baseUrl}?tab={tabParam}{fragment}";

		NavigationManager.NavigateTo(newUrl, replace: true);
	}

	private async Task ScrollToAnchorAsync()
	{
		var uri = new Uri(NavigationManager.Uri);
		if (!string.IsNullOrEmpty(uri.Fragment))
		{
			var anchor = uri.Fragment.TrimStart('#');
			await JSRuntime.InvokeVoidAsync("scrollToElement", anchor).ConfigureAwait(true);
		}
	}

	public void Dispose()
	{
		NavigationManager.LocationChanged -= OnLocationChanged;
		GC.SuppressFinalize(this);
	}
}
