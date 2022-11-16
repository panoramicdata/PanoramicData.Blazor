namespace PanoramicData.Blazor;

public partial class PDZoomBar : IAsyncDisposable
{
	private static int _seq;
	private PDCanvas _canvas = null!;
	private DotNetObjectReference<PDZoomBar>? _objRef;
	private IJSObjectReference? _module;

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter]
	public string Id { get; set; } = $"pd-zoombar-{++_seq}";

	[Parameter]
	public ZoomBarOptions Options { get; set; } = new ZoomBarOptions();

	[Parameter]
	public ZoombarValue Value { get; set; } = new ZoombarValue();

	[Parameter]
	public EventCallback<ZoombarValue> ValueChanged { get; set; }

	[Parameter]
	public int Width { get; set; } = 200;

	private string CanvasId => $"{Id}-canvas";

	private int Height => 20;

	private bool CanZoomIn()
	{
		return Options.ZoomSteps.Length > 0 && Value.Zoom != Options.ZoomSteps[0];
	}

	private bool CanZoomOut()
	{
		return Options.ZoomSteps.Length > 0 && Value.Zoom != Options.ZoomSteps.Last();
	}

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_objRef = DotNetObjectReference.Create(this);
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDZoomBar.razor.js");
			if (_module != null)
			{
				await _module.InvokeVoidAsync("initialize", CanvasId, Value, Options, _objRef).ConfigureAwait(true);
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

	[JSInvokable]
	public async Task OnValueChanged(ZoombarValue value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
	}

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