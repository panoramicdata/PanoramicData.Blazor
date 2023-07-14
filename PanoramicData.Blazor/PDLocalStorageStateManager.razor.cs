namespace PanoramicData.Blazor;

public partial class PDLocalStorageStateManager : IAsyncStateManager
{
	private IJSObjectReference? _module;
	private TaskCompletionSource<bool> _initializationTaskCompletionSource = new();

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_module is null && JSRuntime != null)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDLocalStorageStateManager.razor.js").ConfigureAwait(true);
			_initializationTaskCompletionSource.SetResult(true);
		}
	}

	#region IAsyncStateManager

	public Task InitializeAsync() => _initializationTaskCompletionSource.Task;

	public async Task<T?> LoadStateAsync<T>(string key)
	{
		try
		{
			if (JSRuntime is null || _module is null)
			{
				throw new InvalidOperationException("Javascript runtime is not available");
			}
			var data = await _module.InvokeAsync<string>("getItem", key);
			if (data == null)
			{
				return default;
			}
			return System.Text.Json.JsonSerializer.Deserialize<T>(data);
		}
		catch (Exception e)
		{
			throw new StateException("Failed to save state: see inner exception for more information", e);
		}
	}

	public async Task RemoveStateAsync(string key)
	{
		try
		{
			if (JSRuntime is null || _module is null)
			{
				throw new InvalidOperationException("Javascript runtime is not available");
			}
			await _module.InvokeVoidAsync("removeItem", key);
		}
		catch (Exception e)
		{
			throw new StateException("Failed to save state: see inner exception for more information", e);
		}
	}

	public async Task SaveStateAsync(string key, object state)
	{
		try
		{
			if (JSRuntime is null || _module is null)
			{
				throw new InvalidOperationException("Javascript runtime is not available");
			}
			var data = System.Text.Json.JsonSerializer.Serialize(state);
			await _module.InvokeVoidAsync("setItem", key, data);
		}
		catch (Exception e)
		{
			throw new StateException("Failed to save state: see inner exception for more information", e);
		}
	}

	#endregion
}
