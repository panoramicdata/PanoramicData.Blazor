namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a dual-handle range slider for selecting a numeric range.
/// </summary>
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

	/// <summary>
	/// Gets or sets the start handle element, set by the component markup.
	/// </summary>
	internal ElementReference SvgRangeHandleStart { get; set; }

	/// <summary>
	/// Gets or sets the end handle element, set by the component markup.
	/// </summary>
	internal ElementReference SvgRangeHandleEnd { get; set; }

	#region Injected

	/// <summary>
	/// Gets or sets the injected JavaScript runtime.
	/// </summary>
	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	#endregion

	#region Parameters

	/// <summary>
	/// Gets or sets the height of the component.
	/// </summary>
	[Parameter]
	public double Height { get; set; } = 30;

	/// <summary>
	/// Gets or sets whether to invert the range.
	/// </summary>
	[Parameter]
	public bool Invert { get; set; }

	/// <summary>
	/// Gets or sets the options for the range component.
	/// </summary>
	[Parameter]
	public RangeOptions Options { get; set; } = new();

	/// <summary>
	/// Gets or sets the numeric range.
	/// </summary>
	[Parameter]
	public NumericRange Range { get; set; } = new();

	/// <summary>
	/// Gets or sets whether to show labels.
	/// </summary>
	[Parameter]
	public bool ShowLabels { get; set; }

	/// <summary>
	/// Gets or sets the major tick interval.
	/// </summary>
	[Parameter]
	public double TickMajor { get; set; }

	/// <summary>
	/// A function to format the major tick labels.
	/// </summary>
	[Parameter]
	public Func<double, string>? TickMajorLabelFn { get; set; }

	/// <summary>
	/// Gets or sets the maximum value of the range.
	/// </summary>
	[Parameter]
	public double Max { get; set; } = 100;

	/// <summary>
	/// Gets or sets the minimum value of the range.
	/// </summary>
	[Parameter]
	public double Min { get; set; }

	/// <summary>
	/// Gets or sets the minimum gap between the start and end of the range.
	/// </summary>
	[Parameter]
	public double MinGap { get; set; }

	/// <summary>
	/// An event callback that is invoked when the range changes.
	/// </summary>
	[Parameter]
	public EventCallback<NumericRange> RangeChanged { get; set; }

	/// <summary>
	/// Gets or sets the step value for the range.
	/// </summary>
	[Parameter]
	public double Step { get; set; }

	/// <summary>
	/// Gets or sets the height of the track.
	/// </summary>
	[Parameter]
	public double TrackHeight { get; set; } = 0.75;

	/// <summary>
	/// Gets or sets the width of the component.
	/// </summary>
	[Parameter]
	public double Width { get; set; } = 400;

	#endregion

	#region Calculated Properties

	private double CalcStartHandleX => 1 + Math.Round(CalcFraction(Range.Start) * CalcTrackWidth, 2);

	private double CalcEndHandleX => 1 + Math.Round(CalcFraction(Range.End) * CalcTrackWidth, 2);

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

	/// <summary>
	/// The position of a value along the track as a fraction of the Min to Max span, so Min is at 0 and Max at 1.
	/// </summary>
	private double CalcFraction(double value) => Max > Min ? (value - Min) / (Max - Min) : 0;

	/// <summary>
	/// The x position of the major tick for a value.
	/// </summary>
	private double CalcTickX(double value) => CalcTrackStart + (CalcRangePixels * (value - Min));

	/// <inheritdoc />
	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
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
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, SvgRangeHandleEnd).ConfigureAwait(true);
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
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, SvgRangeHandleStart).ConfigureAwait(true);
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

	private void OnHandlePointerUp()
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

	/// <summary>Validates and clamps the current range values to the configured min, max, and step constraints.</summary>
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

		// which also keeps the end at or after the start
		if (Range.Start > Range.End)
		{
			Range.Start = Range.End;
		}

		// constrain end
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

	/// <inheritdoc />
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
			// BC-40 - the circuit may already be gone, in which case there is no JavaScript side left to tear down
		}
	}

	#endregion
}
