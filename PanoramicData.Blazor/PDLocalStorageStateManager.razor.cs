namespace PanoramicData.Blazor;

public partial class PDLocalStorageStateManager : IAsyncStateManager, IAsyncDisposable
{
	private IJSObjectReference? _module;
	private bool _isInitialized = false;
	private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	#region IAsyncStateManager

	public async Task InitializeAsync()
	{
		if (!_isInitialized)
		{
			if (_module is null && JSRuntime != null)
			{
				await _initializationSemaphore.WaitAsync();
				try
				{
					if (!_isInitialized) // Double-check locking
					{
						_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDLocalStorageStateManager.razor.js").ConfigureAwait(true);
						_isInitialized = true;
					}
					else
					{
					}
				}
				finally
				{
					_initializationSemaphore.Release();
				}
			}
		}
	}

	protected override async Task OnInitializedAsync()
	{
		if (!_isInitialized)
		{
			await InitializeAsync();
		}
	}

	public async Task<T?> LoadStateAsync<T>(string key)
	{
		try
		{
			if (!_isInitialized)
			{
				await InitializeAsync();
			}

			if (_module is null)
			{
				throw new InvalidOperationException("Javascript Module has not been loaded");
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
			if (!_isInitialized)
			{
				await InitializeAsync();
			}

			if (_module is null)
			{
				throw new InvalidOperationException("Javascript Module has not been loaded");
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
			if (!_isInitialized)
			{
				await InitializeAsync();
			}

			if (_module is null)
			{
				throw new InvalidOperationException("Javascript Module has not been loaded");
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
