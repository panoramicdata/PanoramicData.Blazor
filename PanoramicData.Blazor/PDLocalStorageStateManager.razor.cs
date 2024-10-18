using System.Text.Json;

namespace PanoramicData.Blazor;

public partial class PDLocalStorageStateManager : IAsyncStateManager, IAsyncDisposable
{
	private IJSObjectReference? _module;

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	#region IAsyncStateManager

	public async Task InitializeAsync()
	{
		if (_module is null && JSRuntime != null)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDLocalStorageStateManager.razor.js").ConfigureAwait(true);
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await InitializeAsync();
		}
	}

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

			return JsonSerializer.Deserialize<T>(data);
		}
		catch (Exception e)
		{
			throw new StateException("Failed to load state: see inner exception for more information", e);
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
			throw new StateException("Failed to remove state: see inner exception for more information", e);
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

	#region IAsyncDisposable

	public async ValueTask DisposeAsync()
	{
		if (_module != null)
		{
			await _module.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}

	#endregion
}
