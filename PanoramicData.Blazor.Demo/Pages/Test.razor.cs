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
				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch (JSDisconnectedException)
		{
			// The circuit has gone, so the browser has already released the module.
		}
	}

	protected async override Task OnInitializedAsync()
	{
		if (JSRuntime != null)
		{
			try
			{
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor.Demo/Pages/Test.razor.js");
			}
			catch (JSDisconnectedException)
			{
				// The circuit is gone, so there is nothing left to do.
				return;
			}
			try
			{
				await _module.InvokeVoidAsync("init").ConfigureAwait(true);
			}
			catch (JSDisconnectedException)
			{
				// The circuit is gone, so there is nothing left to do.
			}
		}
	}
}