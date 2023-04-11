namespace PanoramicData.Blazor.Demo.Pages;

public partial class Test : IAsyncDisposable
{
	private IJSObjectReference? _module;

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				//await _module.InvokeVoidAsync("hideMenu", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

	protected async override Task OnInitializedAsync()
	{
		if (JSRuntime != null)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor.Demo/Pages/Test.razor.js");
			await _module.InvokeVoidAsync("init").ConfigureAwait(true);
		}
	}
}