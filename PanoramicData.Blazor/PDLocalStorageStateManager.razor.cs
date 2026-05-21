using System.Text.Json;

namespace PanoramicData.Blazor;

/// <summary>
/// State manager component that persists keyed state in browser local storage.
/// </summary>
public partial class PDLocalStorageStateManager : IAsyncStateManager, IAsyncDisposable
{
	private IJSObjectReference? _module;

	/// <summary>
	/// Gets or sets JavaScript runtime used for local-storage interop.
	/// </summary>
	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	/// <summary>
	/// Gets or sets the child content of the component.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	#region IAsyncStateManager

	/// <summary>
	/// Initializes JavaScript module references required by this state manager.
	/// </summary>
	/// <returns>An initialization task.</returns>
	public async Task InitializeAsync()
	{
		if (_module is null && JSRuntime != null)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDLocalStorageStateManager.razor.js").ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Performs first-render initialization.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await InitializeAsync();
		}
	}

	/// <summary>
	/// Loads state for the supplied key from local storage.
	/// </summary>
	/// <typeparam name="T">State type.</typeparam>
	/// <param name="key">State key.</param>
	/// <returns>Deserialized state value, or default when key is missing.</returns>
	public async Task<T?> LoadStateAsync<T>(string key)
	{
		try
		{
			if (JSRuntime is null || _module is null)
			{
				throw new InvalidOperationException("JavaScript runtime is not available");
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

	/// <summary>
	/// Removes state for the supplied key from local storage.
	/// </summary>
	/// <param name="key">State key.</param>
	/// <returns>A removal task.</returns>
	public async Task RemoveStateAsync(string key)
	{
		try
		{
			if (JSRuntime is null || _module is null)
			{
				throw new InvalidOperationException("JavaScript runtime is not available");
			}

			await _module.InvokeVoidAsync("removeItem", key);
		}
		catch (Exception e)
		{
			throw new StateException("Failed to remove state: see inner exception for more information", e);
		}
	}

	/// <summary>
	/// Saves state for the supplied key to local storage.
	/// </summary>
	/// <param name="key">State key.</param>
	/// <param name="state">State object to serialize and store.</param>
	/// <returns>A save task.</returns>
	public async Task SaveStateAsync(string key, object state)
	{
		try
		{
			if (JSRuntime is null || _module is null)
			{
				throw new InvalidOperationException("JavaScript runtime is not available");
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

	/// <summary>
	/// Disposes JavaScript module resources.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		try
		{
			if (_module != null)
			{
				await _module.DisposeAsync();
			}
		}
		catch (JSDisconnectedException)
		{
			// Ignore the exception if the JS runtime is disconnected
		}
		finally
		{
			_module = null;
		}

		GC.SuppressFinalize(this);
	}

	#endregion
}
