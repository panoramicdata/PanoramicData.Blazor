namespace PanoramicData.Blazor;

/// <summary>
/// Base class for Blazor components that need to load JavaScript modules.
/// Provides common functionality for loading and disposing JavaScript modules.
/// </summary>
public abstract class JSModuleComponentBase : ComponentBase, IAsyncDisposable
{
	/// <summary>Gets the <see cref="IJSRuntime"/> injected by the Blazor framework.</summary>
	[Inject] protected IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>Gets the loaded JavaScript module reference, or <c>null</c> before the module has been imported.</summary>
	protected IJSObjectReference? Module { get; private set; }

	/// <summary>
	/// Gets the JavaScript module path for this component.
	/// </summary>
	protected abstract string ModulePath { get; }

	/// <summary>
	/// Called after the JavaScript module is successfully loaded.
	/// Override this method to perform initialization that requires the module.
	/// </summary>
	/// <param name="firstRender">True if this is the first render of the component.</param>
	protected virtual Task OnModuleLoadedAsync(bool firstRender) => Task.CompletedTask;

	/// <summary>
	/// Called after every render when the module is available.
	/// Override this method to perform actions that should happen after each render.
	/// </summary>
	/// <param name="firstRender">True if this is the first render of the component.</param>
	protected virtual Task OnAfterRenderWithModuleAsync(bool firstRender) => Task.CompletedTask;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				Module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(true);
				await OnModuleLoadedAsync(firstRender);
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}

		if (Module is not null)
		{
			try
			{
				await OnAfterRenderWithModuleAsync(firstRender);
			}
			catch
			{
				// Ignore JavaScript errors if the element is not available
			}
		}
	}

	/// <summary>
	/// Disposes the loaded JavaScript module and suppresses the finalizer.
	/// </summary>
	public virtual async ValueTask DisposeAsync()
	{
		try
		{
			if (Module is not null)
			{
				await Module.DisposeAsync();
				Module = null;
			}
		}
		catch
		{
			// Ignore disposal errors
		}

		GC.SuppressFinalize(this);
	}
}