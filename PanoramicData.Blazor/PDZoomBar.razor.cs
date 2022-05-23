namespace PanoramicData.Blazor;

public partial class PDZoomBar : IDisposable
{
	private static int _seq;
	private PDCanvas _canvas = null!;
	private DotNetObjectReference<PDZoomBar>? _objRef;

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

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
			await JSRuntime.InvokeVoidAsync("panoramicData.zoombar.init", CanvasId, Value, Options, _objRef).ConfigureAwait(true);
		}
	}

	private async Task OnZoomIn()
	{
		var idx = Array.IndexOf(Options.ZoomSteps, Value.Zoom);
		if (idx > 0)
		{
			Value.Zoom = Options.ZoomSteps[idx - 1];
			await JSRuntime.InvokeVoidAsync("panoramicData.zoombar.setValue", CanvasId, Value).ConfigureAwait(true);
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}
	}

	private async Task OnZoomOut()
	{
		var idx = Array.IndexOf(Options.ZoomSteps, Value.Zoom);
		if (idx < Options.ZoomSteps.Length - 1)
		{
			Value.Zoom = Options.ZoomSteps[idx + 1];
			await JSRuntime.InvokeVoidAsync("panoramicData.zoombar.setValue", CanvasId, Value).ConfigureAwait(true);
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}
	}

	[JSInvokable]
	public async Task OnValueChanged(ZoombarValue value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
	}

	public void Dispose()
	{
		JSRuntime.InvokeVoidAsync("panoramicData.zoombar.term", CanvasId);
	}
}
