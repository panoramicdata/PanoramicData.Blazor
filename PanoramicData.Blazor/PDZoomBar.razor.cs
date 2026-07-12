namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a zoom bar control for pan and zoom navigation.
/// </summary>
public partial class PDZoomBar : IAsyncDisposable
{
	private static int _seq;
	private PDCanvas _canvas = null!;
	private DotNetObjectReference<PDZoomBar>? _objRef;
	private IJSObjectReference? _module;

	/// <summary>
	/// Gets the injected JavaScript runtime.
	/// </summary>
	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the unique identifier for the component.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-zoombar-{++_seq}";

	/// <summary>
	/// Gets or sets the options for the zoom bar.
	/// </summary>
	[Parameter]
	public ZoomBarOptions Options { get; set; } = new ZoomBarOptions();

	/// <summary>
	/// Gets or sets the current zoom and pan value.
	/// </summary>
	[Parameter]
	public ZoombarValue Value { get; set; } = new ZoombarValue();

	/// <summary>
	/// An event callback that is invoked when the zoom or pan value changes.
	/// </summary>
	[Parameter]
	public EventCallback<ZoombarValue> ValueChanged { get; set; }

	/// <summary>
	/// Gets or sets the width of the zoom bar canvas.
	/// </summary>
	[Parameter]
	public int Width { get; set; } = 200;

	private string CanvasId => $"{Id}-canvas";

	private static int Height => 20;

	private bool CanZoomIn() => Options.ZoomSteps.Length > 0 && Value.Zoom != Options.ZoomSteps[0];

	private bool CanZoomOut() => Options.ZoomSteps.Length > 0 && Value.Zoom != Options.ZoomSteps.Last();

	/// <inheritdoc />
	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_objRef = DotNetObjectReference.Create(this);
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDZoomBar.razor.js");
				if (_module != null)
				{
					await _module.InvokeVoidAsync("initialize", CanvasId, Value, Options, _objRef).ConfigureAwait(true);
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	private async Task OnZoomIn()
	{
		var idx = Array.IndexOf(Options.ZoomSteps, Value.Zoom);
		if (idx > 0)
		{
			Value.Zoom = Options.ZoomSteps[idx - 1];
			if (_module != null)
			{
				await _module.InvokeVoidAsync("setValue", CanvasId, Value).ConfigureAwait(true);
			}

			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}
	}

	private async Task OnZoomOut()
	{
		var idx = Array.IndexOf(Options.ZoomSteps, Value.Zoom);
		if (idx < Options.ZoomSteps.Length - 1)
		{
			Value.Zoom = Options.ZoomSteps[idx + 1];
			if (_module != null)
			{
				await _module.InvokeVoidAsync("setValue", CanvasId, Value).ConfigureAwait(true);
			}

			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Invoked from JavaScript when the zoom or pan value changes.
	/// </summary>
	/// <param name="value">The new zoom/pan value reported by the JavaScript zoom bar.</param>
	[JSInvokable]
	public async Task OnValueChanged(ZoombarValue value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				await _module.InvokeVoidAsync("dispose", CanvasId).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}

			_objRef?.Dispose();
		}
		catch
		{
		}
	}
}