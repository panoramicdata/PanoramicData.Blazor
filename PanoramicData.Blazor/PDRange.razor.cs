namespace PanoramicData.Blazor;

public partial class PDRange : IAsyncDisposable
{
	private const double HandleWidth = 10;

	private bool _isDragging;
	private double _dragOrigin;
	private IJSObjectReference? _commonModule;
	private ElementReference _svgRangeHandleStart;
	private ElementReference _svgRangeHandleEnd;
	private bool _disposedValue;

	#region Injected

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	#endregion

	#region Parameters

	[Parameter]
	public double Height { get; set; } = 20;

	[Parameter]
	public bool Invert { get; set; }

	[Parameter]
	public RangeOptions Options { get; set; } = new();

	[Parameter]
	public NumericRange Range { get; set; } = new();

	[Parameter]
	public double Max { get; set; } = default;

	[Parameter]
	public double Min { get; set; } = default;

	[Parameter]
	public EventCallback<NumericRange> RangeChanged { get; set; }

	[Parameter]
	public double Step { get; set; } = default;

	[Parameter]
	public double TrackHeight { get; set; } = 0.5;

	[Parameter]
	public double Width { get; set; } = 400;

	#endregion

	#region Calculated Properties

	private double CalcStartHandleX => 1 + Math.Round((Range.Start / Max) * CalcTrackWidth, 2);

	private double CalcEndHandleX => 1 + Math.Round((Range.End / Max) * CalcTrackWidth, 2);

	private double CalcTrackHeight => TrackHeight * Height;

	private double CalcTrackWidth => Width - HandleWidth - 2;

	private double CalcTrackY => (Height / 2) - (CalcTrackHeight / 2);

	#endregion

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			//_objRef = DotNetObjectReference.Create(this);
			//_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDTimeline.razor.js").ConfigureAwait(true);
			//if (_module != null)
			//{
			//	await _module.InvokeVoidAsync("initialize", Id, Options, _objRef).ConfigureAwait(true);
			//}

			_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js");
			//if (_commonModule != null)
			//{
			//	_canvasHeight = (int)(await _commonModule.InvokeAsync<double>("getHeight", _svgPlotElement).ConfigureAwait(true));
			//	_canvasWidth = (int)(await _commonModule.InvokeAsync<double>("getWidth", _svgPlotElement).ConfigureAwait(true));
			//	_canvasX = (int)(await _commonModule.InvokeAsync<double>("getX", _svgPlotElement).ConfigureAwait(true));
			//}
		}
	}

	private async Task OnRangeStartPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && !_isDragging)
		{
			_isDragging = true;
			_dragOrigin = args.OffsetX; //args.ClientX;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgRangeHandleStart).ConfigureAwait(true);
			}
		}
	}

	private async Task OnRangeStartPointerMove(PointerEventArgs args)
	{
		if (_isDragging)
		{
			_dragOrigin = args.OffsetX; //args.ClientX;
										// calculate position as a percentage

		}
	}

	private async Task OnRangeStartPointerUp(PointerEventArgs args)
	{
		if (_isDragging)
		{
			_isDragging = false;
		}
	}

	protected override void Validate()
	{
		base.Validate();
		base.FluentValidate(new PDRangeValidator(), this);
	}

	#region IAsyncDisposable

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

	#endregion
}
