namespace PanoramicData.Blazor;

public partial class PDRange : IAsyncDisposable
{
	private const double _handleWidth = 10;
	private const double _labelSplit = 0.333;
	private const double _trackSplit = 0.666;

	private bool _isDragging;
	private double _dragX;
	private double _dragPixelOrigin;
	private double _dragRangeOrigin;
	private IJSObjectReference? _commonModule;
	private ElementReference _svgRangeHandleStart;
	private ElementReference _svgRangeHandleEnd;

	#region Injected

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	#endregion

	#region Parameters

	[Parameter]
	public double Height { get; set; } = 30;

	[Parameter]
	public bool Invert { get; set; }

	[Parameter]
	public RangeOptions Options { get; set; } = new();

	[Parameter]
	public NumericRange Range { get; set; } = new();

	[Parameter]
	public bool ShowLabels { get; set; }

	[Parameter]
	public double TickMajor { get; set; }

	[Parameter]
	public Func<double, string>? TickMajorLabelFn { get; set; }

	[Parameter]
	public double Max { get; set; } = 100;

	[Parameter]
	public double Min { get; set; }

	[Parameter]
	public double MinGap { get; set; }

	[Parameter]
	public EventCallback<NumericRange> RangeChanged { get; set; }

	[Parameter]
	public double Step { get; set; }

	[Parameter]
	public double TrackHeight { get; set; } = 0.75;

	[Parameter]
	public double Width { get; set; } = 400;

	#endregion

	#region Calculated Properties

	private double CalcStartHandleX => 1 + Math.Round((Range.Start / Max) * CalcTrackWidth, 2);

	private double CalcEndHandleX => 1 + Math.Round((Range.End / Max) * CalcTrackWidth, 2);

	private double CalcHandleHeight => ShowLabels ? Height * _trackSplit : Height;

	private double CalcHandleY => ShowLabels ? Height * _labelSplit : 0;

	private double CalcTrackHeight => (ShowLabels ? _trackSplit * TrackHeight : TrackHeight) * Height;

	private static double CalcTrackStart => 1 + _handleWidth / 2;

	private double CalcRangePixels => (CalcTrackWidth / (Max - Min));

	private double CalcTrackWidth => Width - _handleWidth - 2;

	private double CalcTrackY => ShowLabels
		? (((Height * _trackSplit) / 2) - (CalcTrackHeight / 2)) + (Height * _labelSplit)
		: (Height / 2) - (CalcTrackHeight / 2);

	#endregion

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js");
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}
	private async Task OnEndHandlePointerDown(PointerEventArgs args)
	{
		if (IsEnabled && !_isDragging)
		{
			_isDragging = true;
			_dragPixelOrigin = _dragX = args.OffsetX;
			_dragRangeOrigin = Range.End;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgRangeHandleEnd).ConfigureAwait(true);
			}
		}
	}

	private Task OnEndHandlePointerMove(PointerEventArgs args)
	{
		if (_isDragging)
		{
			// update Range.Start
			_dragX = args.OffsetX;
			var delta = _dragX - _dragPixelOrigin;
			var rangeDelta = delta * ((Max - Min) / CalcTrackWidth);
			var newEnd = _dragRangeOrigin + rangeDelta;

			// constrain to Range.Start - Max
			if (newEnd < Range.Start)
			{
				newEnd = Range.Start;
			}

			if (newEnd > Max)
			{
				newEnd = Max;
			}

			// constrain to MinGap
			if (MinGap > 0 && newEnd - Range.Start < MinGap)
			{
				newEnd = Range.Start + MinGap;
			}

			// snap to step?
			if (Step > 0)
			{
				newEnd = Math.Round(newEnd / Step) * Step;
			}

			// update Range
			return UpdateRange(null, newEnd);
		}

		return Task.CompletedTask;
	}


	private async Task OnStartHandlePointerDown(PointerEventArgs args)
	{
		if (IsEnabled && !_isDragging)
		{
			_isDragging = true;
			_dragPixelOrigin = _dragX = args.OffsetX;
			_dragRangeOrigin = Range.Start;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgRangeHandleStart).ConfigureAwait(true);
			}
		}
	}

	private Task OnStartHandlePointerMove(PointerEventArgs args)
	{
		if (_isDragging)
		{
			// update Range.Start
			_dragX = args.OffsetX;
			var delta = _dragX - _dragPixelOrigin;
			var rangeDelta = delta * ((Max - Min) / CalcTrackWidth);
			var newStart = _dragRangeOrigin + rangeDelta;

			// constrain to Min - Range.End
			if (newStart < Min)
			{
				newStart = Min;
			}

			if (newStart > Range.End)
			{
				newStart = Range.End;
			}

			// constrain to MinGap
			if (MinGap > 0 && Range.End - newStart < MinGap)
			{
				newStart = Range.End - MinGap;
			}

			// snap to step?
			if (Step > 0)
			{
				newStart = Math.Round(newStart / Step) * Step;
			}

			// update Range
			return UpdateRange(newStart, null);
		}

		return Task.CompletedTask;
	}

	private void OnHandlePointerUp(PointerEventArgs args)
	{
		if (_isDragging)
		{
			_isDragging = false;
		}
	}

	private async Task UpdateRange(double? start, double? end)
	{
		if (start.HasValue)
		{
			Range.Start = start.Value;
		}

		if (end.HasValue)
		{
			Range.End = end.Value;
		}

		if (start.HasValue || end.HasValue)
		{
			await RangeChanged.InvokeAsync(Range).ConfigureAwait(true);
		}
	}

	protected override void Validate()
	{
		// snap to step size?
		if (Step > 0)
		{
			Range.Start = Math.Round(Range.Start / Step) * Step;
			Range.End = Math.Round(Range.End / Step) * Step;
		}

		// constrain start
		if (Range.Start < Min)
		{
			Range.Start = Min;
		}

		if (Range.Start > Range.End)
		{
			Range.Start = Range.End;
		}

		// constrain to end
		if (Range.End < Range.Start)
		{
			Range.End = Range.Start;
		}

		if (Range.End > Max)
		{
			Range.End = Max;
		}

		// constrain to MinGap
		if (MinGap > 0 && Range.End - Range.Start < MinGap)
		{
			if (Range.End - MinGap >= Min)
			{
				Range.Start = Range.End - MinGap;
			}
			else
			{
				Range.End = Range.Start + MinGap;
			}
		}

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
