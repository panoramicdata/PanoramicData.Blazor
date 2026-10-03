namespace PanoramicData.Blazor;

/// <summary>
/// Timeline component that supports zooming, panning, range selection, and data-provider driven rendering.
/// </summary>
public partial class PDTimeline : IAsyncDisposable, IEnablable
{
	/// <summary>
	/// Delegate used to fetch timeline data for a date range and scale.
	/// </summary>
	/// <param name="start">Start of requested range.</param>
	/// <param name="end">End of requested range.</param>
	/// <param name="scale">Timeline scale for the request.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Data points for the requested range.</returns>
	public delegate ValueTask<DataPoint[]> DataProviderDelegate(DateTime start, DateTime end, TimelineScale scale, CancellationToken cancellationToken);

	private static int _seq;

	private int _canvasHeight;
	private int _canvasWidth;
	private int _canvasX;
	private int _columnOffset;

	private bool _isChartDragging;
	private bool _isPotentialDrag; // Track if pointer is down but not yet dragging
	private bool _isDraggingSelection; // Track if dragging entire selection with modifier key
	private int _dragSelectionStartOffset; // Original start index when drag began
	private int _dragSelectionEndOffset; // Original end index when drag began
	private double _chartDragStartX;
	private const double _dragThreshold = 5.0; // pixels
	private int _lastSelectionStartIndex;
	private int _lastSelectionEndIndex;
	private TimeRange? _selectionRange;
	private bool _isSelectionStartDragging;
	private bool _isSelectionEndDragging;

	private bool _isPanDragging;
	private double _panDragOrigin;
	private double _panHandleWidth;
	private double _panHandleX;

	private IJSObjectReference? _module;
	private DotNetObjectReference<PDTimeline>? _objRef;
	private bool _loading;
	private DateTime _lastMinDateTime;
	private DateTime? _lastMaxDateTime;
	private IJSObjectReference? _commonModule;
	private readonly Dictionary<int, DataPoint> _dataPoints = [];

	private bool _disposed;

	/// <summary>
	/// Gets or sets the JavaScript runtime used by this component.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the date and time after which the timeline is disabled.
	/// </summary>
	[Parameter]
	public DateTime DisableAfter { get; set; }

	/// <summary>
	/// Gets or sets the date and time before which the timeline is disabled.
	/// </summary>
	[Parameter]
	public DateTime DisableBefore { get; set; }

	/// <summary>
	/// An event callback that is invoked when the component has been initialized.
	/// </summary>
	[Parameter]
	public EventCallback Initialized { get; set; }

	/// <summary>
	/// Gets or sets whether the timeline is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the right-hand edge should remain anchored to the current time.
	/// This is opt-in and defaults to false to preserve the existing static timeline behaviour.
	/// Direct user navigation suspends following until <see cref="ResumeFollowNowAsync"/> is called.
	/// </summary>
	[Parameter]
	public bool FollowNow { get; set; }

	/// <summary>
	/// An event callback that is invoked when following is resumed or suspended.
	/// </summary>
	[Parameter]
	public EventCallback<bool> FollowNowChanged { get; set; }

	/// <summary>
	/// Gets or sets how frequently the component checks whether the current scale has advanced.
	/// </summary>
	[Parameter]
	public TimeSpan FollowNowRefreshInterval { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Gets or sets whether an existing selection should roll forward while following now.
	/// </summary>
	[Parameter]
	public bool FollowNowSelection { get; set; } = true;

	/// <summary>
	/// Gets or sets the clock used by live following. The default is the system clock.
	/// </summary>
	[Parameter]
	public TimeProvider Clock { get; set; } = TimeProvider.System;

	/// <summary>
	/// Gets or sets the current scale of the timeline.
	/// </summary>
	[Parameter]
	public TimelineScale Scale { get; set; } = TimelineScale.Years;

	/// <summary>
	/// An event callback that is invoked when the timeline scale changes.
	/// </summary>
	/// <remarks>
	/// It is not raised when the timeline first lays itself out at the <see cref="Scale"/> it was given, since
	/// that is not a change. Use <see cref="Initialized"/> to learn when the timeline is ready.
	/// </remarks>
	[Parameter]
	public EventCallback<TimelineScale> ScaleChanged { get; set; }

	/// <summary>
	/// An event callback that is invoked when the timeline has been refreshed.
	/// </summary>
	[Parameter]
	public EventCallback Refreshed { get; set; }

	/// <summary>
	/// An event callback that is invoked when the time selection changes.
	/// </summary>
	[Parameter]
	public EventCallback<TimeRange?> SelectionChanged { get; set; }

	/// <summary>
	/// An event callback that is invoked when the time selection change is complete.
	/// </summary>
	[Parameter]
	public EventCallback SelectionChangeEnd { get; set; }

	/// <summary>
	/// A delegate that provides data points to the timeline.
	/// </summary>
	[Parameter]
	public DataProviderDelegate? DataProvider { get; set; }

	/// <summary>
	/// When true (the default, preserving the original behaviour) a browser resize forces a full data re-fetch.
	/// When false a resize only re-lays-out the timeline; data is re-fetched only if the visible range actually
	/// changed (the normal unchanged-range guard still applies). Set this to false for timelines that fetch the
	/// whole window, where a resize never changes what data is needed.
	/// </summary>
	[Parameter]
	public bool RefetchDataOnResize { get; set; } = true;

	/// <summary>
	/// Gets or sets the unique identifier for the component.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-timeline-{++_seq}";

	/// <summary>
	/// Gets or sets whether a new maximum date/time is available.
	/// </summary>
	[Parameter]
	public bool NewMaxDateTimeAvailable { get; set; }

	/// <summary>
	/// Gets or sets whether a new minimum date/time is available.
	/// </summary>
	[Parameter]
	public bool NewMinDateTimeAvailable { get; set; }

	/// <summary>
	/// Gets or sets the maximum date and time of the timeline.
	/// </summary>
	[Parameter]
	public DateTime? MaxDateTime { get; set; }

	/// <summary>
	/// Gets or sets the minimum date and time of the timeline.
	/// </summary>
	[Parameter]
	public DateTime MinDateTime { get; set; }

	/// <summary>
	/// Gets or sets the options for the timeline.
	/// </summary>
	[Parameter]
	public TimelineOptions Options { get; set; } = new TimelineOptions();

	/// <summary>
	/// An event callback that is invoked to update the maximum date.
	/// </summary>
	[Parameter]
	public EventCallback UpdateMaxDate { get; set; }

	/// <summary>
	/// An event callback that is invoked to update the minimum date.
	/// </summary>
	[Parameter]
	public EventCallback UpdateMinDate { get; set; }

	/// <summary>
	/// A function to transform the Y value of data points.
	/// </summary>
	[Parameter]
	public Func<double, double> YValueTransform { get; set; } = (v) => v;

	/// <summary>
	/// Gets or sets the plot area element, set by the markup's <c>@ref</c>.
	/// </summary>
	internal ElementReference SvgPlotElement { get; set; }

	/// <summary>
	/// Gets or sets the pan track element, set by the markup's <c>@ref</c>.
	/// </summary>
	internal ElementReference SvgPanElement { get; set; }

	/// <summary>
	/// Gets or sets the selection start handle element, set by the markup's <c>@ref</c>.
	/// </summary>
	internal ElementReference SvgSelectionHandleStart { get; set; }

	/// <summary>
	/// Gets or sets the selection end handle element, set by the markup's <c>@ref</c>.
	/// </summary>
	internal ElementReference SvgSelectionHandleEnd { get; set; }

	/// <summary>
	/// Gets the rounded maximum date for the current scale.
	/// </summary>
	public DateTime RoundedMaxDateTime => Scale.PeriodEnd(MaxDateTime ?? CurrentDateTime);

	/// <summary>
	/// Gets the rounded minimum date for the current scale.
	/// </summary>
	public DateTime RoundedMinDateTime
	{
		get
		{
			return _totalColumns < _viewportColumns && Options.General.RightAlign
				? Scale.AddPeriods(Scale.PeriodStart(MinDateTime), _totalColumns - _viewportColumns)
				: Scale.PeriodStart(MinDateTime);
		}
	}

	private DateTime CurrentDateTime => ReferenceEquals(Clock, TimeProvider.System)
		? DateTime.Now
		: Clock.GetLocalNow().DateTime;

	/// <summary>
	/// Gets the number of whole columns the measured canvas can show, or zero before it has been measured.
	/// </summary>
	private int CanvasColumns => _canvasWidth > 0 ? (int)Math.Floor(_canvasWidth / (double)Options.Bar.Width) : 0;

	/// <summary>
	/// Disposes JavaScript resources and object references used by the timeline.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		_disposed = true;
		try
		{
			_followNowCancellationTokenSource?.Cancel();
			_refreshCancellationToken?.Cancel();
			if (_followNowTask is not null)
			{
				await _followNowTask.ConfigureAwait(true);
			}

			_followNowCancellationTokenSource?.Dispose();
			if (_module != null)
			{
				await _module.InvokeVoidAsync("dispose", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}

			_objRef?.Dispose();
		}
		catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException or ObjectDisposedException)
		{
			// The circuit or page is going away (or the browser side has already released the timeline), so there
			// is nothing left to release and disposal must not fail because of it.
		}
	}

	/// <summary>
	/// Initializes JavaScript interop and initial component sizing after first render.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				await InitializeInteropAsync().ConfigureAwait(true);
				if (Options.General.AutoRefresh)
				{
					await SetScale(Scale, true).ConfigureAwait(true);
				}

				// notify app
				await Initialized.InvokeAsync().ConfigureAwait(true);
				if (_isFollowingNow)
				{
					await RefreshFollowNowAsync(true).ConfigureAwait(true);
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	private async Task InitializeInteropAsync()
	{
		_objRef = DotNetObjectReference.Create(this);
		_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDTimeline.razor.js").ConfigureAwait(true);
		if (_module != null)
		{
			await _module.InvokeVoidAsync("initialize", Id, Options, _objRef).ConfigureAwait(true);
		}

		_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl).ConfigureAwait(true);
		if (_commonModule != null)
		{
			_canvasHeight = (int)await _commonModule.InvokeAsync<double>("getHeight", SvgPlotElement).ConfigureAwait(true);
			_canvasWidth = (int)await _commonModule.InvokeAsync<double>("getWidth", SvgPlotElement).ConfigureAwait(true);
			_canvasX = (int)await _commonModule.InvokeAsync<double>("getX", SvgPlotElement).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Applies parameter-driven refresh and reset behavior.
	/// </summary>
	protected async override Task OnParametersSetAsync()
	{
		if (FollowNowRefreshInterval <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(FollowNowRefreshInterval), "The follow-now refresh interval must be greater than zero.");
		}

		if (Options.General.AutoRefresh)
		{
			await ApplyAutoRefreshParametersAsync().ConfigureAwait(true);
		}

		var followNowParameterChanged = UpdateFollowNowParameter();
		await ApplyFollowNowParameterAsync(followNowParameterChanged).ConfigureAwait(true);
	}

	private async Task ApplyAutoRefreshParametersAsync()
	{
		if (_canvasWidth > 0)
		{
			await SetScale(Scale).ConfigureAwait(true);
		}

		// reset if earliest date changes
		if (MinDateTime != _lastMinDateTime)
		{
			_lastMinDateTime = MinDateTime;
			await Reset().ConfigureAwait(true);
			await SetScale(Scale, true).ConfigureAwait(true);
		}

		// reset if latest date changes
		if (MaxDateTime != _lastMaxDateTime)
		{
			_lastMaxDateTime = MaxDateTime;
			await Reset().ConfigureAwait(true);
			await SetScale(Scale, true).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// JavaScript callback used to handle resize updates.
	/// </summary>
	[JSInvokable("PanoramicData.Blazor.PDTimeline.OnResize")]
	public async Task OnResize()
	{
		if (_commonModule != null)
		{
			_canvasX = (int)(await _commonModule.InvokeAsync<double>("getX", SvgPlotElement).ConfigureAwait(true));
			_canvasWidth = (int)await _commonModule.InvokeAsync<double>("getWidth", SvgPlotElement).ConfigureAwait(true);
		}

		await SetScale(Scale, true, refreshData: RefetchDataOnResize).ConfigureAwait(true);
		await InvokeAsync(() => StateHasChanged()).ConfigureAwait(true);
	}

	/// <summary>
	/// Clears cached data and the current selection.
	/// </summary>
	public Task Clear() => Clear(true);

	/// <summary>
	/// Clears cached data and optionally clears the current selection.
	/// </summary>
	/// <param name="clearSelection">True to clear selection state; otherwise false.</param>
	public async Task Clear(bool clearSelection)
	{
		_dataPoints.Clear();
		if (clearSelection)
		{
			await ClearSelection().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Resets cached data and selection state.
	/// </summary>
	public async Task Reset()
	{
		await Clear().ConfigureAwait(true);
		await ClearSelection().ConfigureAwait(true);
	}

	/// <summary>
	/// Sets the minimum and maximum date bounds.
	/// </summary>
	/// <param name="min">Minimum date.</param>
	/// <param name="max">Maximum date.</param>
	public void SetDates(DateTime min, DateTime max)
	{
		MinDateTime = min;
		MaxDateTime = max;
	}

	/// <summary>
	/// Disables timeline interaction.
	/// </summary>
	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	/// <summary>
	/// Enables timeline interaction.
	/// </summary>
	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	/// <summary>
	/// Sets whether timeline interaction is enabled.
	/// </summary>
	/// <param name="isEnabled">True to enable interaction; otherwise false.</param>
	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}
}
