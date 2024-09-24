namespace PanoramicData.Blazor;

public partial class PDLocalStorageStateManager : IAsyncStateManager, IAsyncDisposable
{
	private IJSObjectReference? _module;
	private bool _isInitialized = false;
	private bool _isInitializing = false;
	private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	#region IAsyncStateManager

	public async Task InitializeAsync()
	{
		Console.WriteLine("PDLocalStorageStateManager.InitializeAsync");
		if (!_isInitialized && !_isInitializing)
		{
			_isInitializing = true;
			if (_module is null && JSRuntime != null)
			{
				Console.WriteLine("PDLocalStorageStateManager.InitializeAsync: Importing module");
				await _initializationSemaphore.WaitAsync();
				try
				{
					if (!_isInitialized) // Double-check locking
					{
						Console.WriteLine("PDLocalStorageStateManager.InitializeAsync: Importing module (inner)");
						_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDLocalStorageStateManager.razor.js").ConfigureAwait(true);
						_isInitialized = true;
					}
				}
				finally
				{
					_initializationSemaphore.Release();
					_isInitializing = false;
				}
			}
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && !_isInitialized)
		{
			Console.WriteLine("PDLocalStorageStateManager.OnAfterRenderAsync: Initializing");
			await InitializeAsync();
		}
	}

	public async Task<T?> LoadStateAsync<T>(string key)
	{
		try
		{
			Console.WriteLine($"PDLocalStorageStateManager.LoadStateAsync: key={key}");
			if (!_isInitialized)
			{
				Console.WriteLine("PDLocalStorageStateManager.LoadStateAsync: Initializing");
				await InitializeAsync();
			}
			if (_module is null)
			{
				Console.WriteLine("PDLocalStorageStateManager.LoadStateAsync: Javascript Module has not been loaded");
				throw new InvalidOperationException("Javascript Module has not been loaded");
			}
			var data = await _module.InvokeAsync<string>("getItem", key);
			if (data == null)
			{
				return default;
			}
			Console.WriteLine($"PDLocalStorageStateManager.LoadStateAsync: data={data}");
			return System.Text.Json.JsonSerializer.Deserialize<T>(data);
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
			throw new StateException("Failed to remove state: see inner exception for more information", e);
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
