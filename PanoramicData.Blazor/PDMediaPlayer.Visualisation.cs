using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor;

public partial class PDMediaPlayer
{
	private const int MinimumSpectrogramWindow = 64;
	private const int MaximumSpectrogramWindow = 8192;
	private const int DefaultSpectrogramWindow = 1024;
	private const int DefaultSpectrogramHop = 256;
	private const int MinimumSpectrogramHeight = 80;
	private const int MaximumSpectrogramHeight = 800;
	private const int DefaultSpectrogramHeight = 260;
	private const int MinimumSpectrogramSampleRate = 3000;
	private const int MaximumSpectrogramSampleRate = 96000;
	private const int DefaultSpectrogramSampleRate = 11025;
	private const double DefaultSpectrogramMaxFrequency = 5500;

	private string? _visualKey;
	private IEnumerable<PDMediaKeyPoint>? _visualKeyPoints;
	private IEnumerable<PDMediaLandmark>? _visualLandmarks;
	private string? _visualStatus;

	/// <summary>
	/// Gets or sets the canvas the visualisation is drawn on, set by the markup's <c>@ref</c>.
	/// </summary>
	internal ElementReference VisualCanvas { get; set; }

	/// <summary>
	/// Gets or sets whether to show a spectrogram of the media under the controls: time across, frequency up, and loudness as
	/// brightness, with labelled axes and a playhead. The browser decodes the media and the player computes the spectrogram
	/// there, so the media must be same-origin or served with CORS headers, and it must be no longer than
	/// <see cref="SpectrogramMaxDuration"/>. Clicking the spectrogram seeks. The default is false.
	/// </summary>
	[Parameter]
	public bool ShowSpectrogram { get; set; }

	/// <summary>
	/// Gets or sets whether to draw <see cref="KeyPoints"/> as dots over the spectrogram (or over an empty plot when
	/// <see cref="ShowSpectrogram"/> is false). The default is false.
	/// </summary>
	[Parameter]
	public bool ShowKeyPoints { get; set; }

	/// <summary>
	/// Gets or sets the key points: notable points in time and frequency, such as spectral peaks. Assign a new collection to change
	/// them. They are only drawn when <see cref="ShowKeyPoints"/> is set.
	/// </summary>
	[Parameter]
	public IEnumerable<PDMediaKeyPoint>? KeyPoints { get; set; }

	/// <summary>
	/// Gets or sets whether to draw <see cref="Landmarks"/> as lines over the spectrogram. The default is false.
	/// </summary>
	[Parameter]
	public bool ShowLandmarks { get; set; }

	/// <summary>
	/// Gets or sets the landmarks: lines between two points in time and frequency, such as audio-fingerprint pairs of peaks. Assign a
	/// new collection to change them. They are only drawn when <see cref="ShowLandmarks"/> is set.
	/// </summary>
	[Parameter]
	public IEnumerable<PDMediaLandmark>? Landmarks { get; set; }

	/// <summary>
	/// Gets or sets whether the frequency labels and the faint lines at each label are drawn inside the plot. The plot always fills
	/// the full width of the visualisation, so that its time axis lines up exactly with the seek bar below it. The default is true.
	/// </summary>
	[Parameter]
	public bool ShowSpectrogramFrequencyLabels { get; set; } = true;

	/// <summary>
	/// Gets or sets the clock time at which the media starts. When null (the default) the time axis shows the position in the media,
	/// as in a song (0:00, 0:10, ...). When set, it shows absolute time, as in a broadcast: each label is this time plus the position
	/// (14:30:00, 14:30:10, ...), at round times, in the offset of the value given.
	/// </summary>
	[Parameter]
	public DateTimeOffset? SpectrogramStartTime { get; set; }

	/// <summary>
	/// Gets or sets whether the controls are shown: the seek bar with its markers and segment lanes, and the buttons, time, volume
	/// and speed. Set it to false for a display-only player, such as a visualisation that follows a live feed. The default is true.
	/// </summary>
	[Parameter]
	public bool ShowControls { get; set; } = true;

	/// <summary>
	/// Gets or sets the length of a sliding window. When set, the visualisation shows only the most recent part of that length,
	/// ending at the playhead or, for a live feed (see <see cref="PushSpectrumAsync"/>), at the latest data; it slides as playback
	/// or the feed advances, and the playhead line is not drawn because the right edge is the present. The default is null, which
	/// shows the whole media. Values are limited to between one second and one hour.
	/// </summary>
	[Parameter]
	public TimeSpan? SpectrogramWindow { get; set; }

	/// <summary>
	/// Gets or sets whether the mouse wheel zooms and pans the visualisation when a modifier key is held: Ctrl (or Cmd) and the wheel
	/// zoom the time axis, Alt and the wheel zoom the frequency axis, Shift and the wheel pan time, and Shift with Alt and the wheel pan
	/// frequency. Zooming is about the point under the pointer. The wheel scrolls the page as usual when no modifier is held. The
	/// default is true.
	/// </summary>
	[Parameter]
	public bool AllowSpectrogramZoom { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the control panel has buttons to zoom and pan the visualisation (they show only when there is a
	/// visualisation and <see cref="ShowControls"/> is set). The default is true.
	/// </summary>
	[Parameter]
	public bool ShowSpectrogramViewControls { get; set; } = true;

	/// <summary>
	/// Gets or sets how the view follows the playhead while the media plays: <see cref="MediaFollowMode.Free"/> (the default) leaves
	/// the view where the zoom and pan put it, <see cref="MediaFollowMode.Margin"/> scrolls to keep the playhead inside
	/// <see cref="SpectrogramFollowMarginPercent"/> of the edges, and <see cref="MediaFollowMode.Centre"/> holds the playhead in the
	/// middle while the media scrolls past. Panning or zooming by hand (wheel or buttons) suspends following until
	/// <see cref="FollowPlayheadAsync"/>, <see cref="ResetViewAsync"/> or a click on the visualisation resumes it.
	/// </summary>
	[Parameter]
	public MediaFollowMode SpectrogramFollowMode { get; set; } = MediaFollowMode.Free;

	/// <summary>
	/// Gets or sets, for <see cref="MediaFollowMode.Margin"/>, the smallest distance the playhead keeps from either edge of the view,
	/// as a percentage of the view's width, from 0 to 45. The default is 15.
	/// </summary>
	[Parameter]
	public double SpectrogramFollowMarginPercent { get; set; } = 15;

	/// <summary>
	/// Gets or sets how <see cref="MediaFollowMode.Margin"/> moves the view: <see cref="MediaFollowScroll.Glide"/> (the default)
	/// continuously, or <see cref="MediaFollowScroll.PageTurn"/> by jumping a screen at a time.
	/// </summary>
	[Parameter]
	public MediaFollowScroll SpectrogramFollowScroll { get; set; } = MediaFollowScroll.Glide;

	/// <summary>
	/// Gets or sets whether following stops scrolling when the end of the media is in view, so that the last screen stays still and the
	/// playhead runs on towards the right edge, past the margin. When false the playhead keeps its place and empty space opens up beyond
	/// the end. The default is true.
	/// </summary>
	[Parameter]
	public bool SpectrogramFollowStopsAtEnd { get; set; } = true;

	/// <summary>
	/// Gets or sets whether an overview bar of the whole media shows under the visualisation, with the part currently shown
	/// highlighted. Drag the highlight to pan, drag its edges or use the wheel to zoom, and click elsewhere on the bar to move the
	/// view there. The default is false.
	/// </summary>
	[Parameter]
	public bool ShowSpectrogramOverview { get; set; }

	/// <summary>
	/// Gets or sets whether the seek bar (with its markers and segment lanes) shows only the part of the media that the visualisation
	/// shows, following its zoom, pan and sliding window. It needs a visualisation to be switched on, and does nothing without one.
	/// The default is false, which shows the whole media.
	/// </summary>
	[Parameter]
	public bool SeekBarFollowsView { get; set; }

	/// <summary>
	/// Gets or sets whether the piano roll (see <see cref="ShowNotes"/>) shows only the part of the media that the visualisation
	/// shows, following its zoom, pan and sliding window. It needs a visualisation to be switched on, and does nothing without one.
	/// The default is false, which shows the whole media.
	/// </summary>
	[Parameter]
	public bool NotesFollowView { get; set; }

	/// <summary>
	/// Gets or sets the height in pixels of the visualisation, from 80 to 800. The default is 260.
	/// </summary>
	[Parameter]
	public int SpectrogramHeight { get; set; } = DefaultSpectrogramHeight;

	/// <summary>
	/// Gets or sets the highest frequency shown, in hertz. It is capped at half <see cref="SpectrogramSampleRate"/>. The default is 5,500.
	/// </summary>
	[Parameter]
	public double SpectrogramMaxFrequency { get; set; } = DefaultSpectrogramMaxFrequency;

	/// <summary>
	/// Gets or sets the sample rate in hertz the media is decoded at before analysis, from 3,000 to 96,000. A lower rate makes the
	/// analysis faster. When the browser cannot decode at the chosen rate it uses its own. The default is 11,025.
	/// </summary>
	[Parameter]
	public int SpectrogramSampleRate { get; set; } = DefaultSpectrogramSampleRate;

	/// <summary>
	/// Gets or sets the length in samples of each analysis window, a power of two from 64 to 8,192 (a value in between is rounded
	/// to the nearest power of two). A longer window separates close frequencies better and blurs time more. The default is 1,024.
	/// </summary>
	[Parameter]
	public int SpectrogramWindowSize { get; set; } = DefaultSpectrogramWindow;

	/// <summary>
	/// Gets or sets how many samples the analysis window advances each step, from 1 to the window size. The default is 256.
	/// </summary>
	[Parameter]
	public int SpectrogramHopSize { get; set; } = DefaultSpectrogramHop;

	/// <summary>
	/// Gets or sets the longest media a spectrogram is made for. Longer media show a message instead, because the whole recording is
	/// decoded and analysed in the browser. The default is 5 minutes.
	/// </summary>
	[Parameter]
	public TimeSpan SpectrogramMaxDuration { get; set; } = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Gets or sets the callback invoked when the user clicks the visualisation, with the time and frequency of the click. The player
	/// also seeks to the time when it can.
	/// </summary>
	[Parameter]
	public EventCallback<PDMediaVisualisationClick> OnVisualisationClick { get; set; }

	/// <summary>
	/// Gets whether any part of the visualisation is switched on.
	/// </summary>
	protected bool HasVisualisation => ShowSpectrogram || ShowKeyPoints || ShowLandmarks;

	/// <summary>
	/// Gets the height of the visualisation after clamping to the allowed range.
	/// </summary>
	internal int SpectrogramHeightValue => Math.Clamp(SpectrogramHeight, MinimumSpectrogramHeight, MaximumSpectrogramHeight);

	/// <summary>
	/// Gets the analysis window length after rounding to a power of two within the allowed range.
	/// </summary>
	internal int SpectrogramWindowValue
	{
		get
		{
			var clamped = Math.Clamp(SpectrogramWindowSize, MinimumSpectrogramWindow, MaximumSpectrogramWindow);
			var lower = 1 << (int)Math.Floor(Math.Log2(clamped));
			var upper = lower << 1;
			return clamped - lower < upper - clamped || upper > MaximumSpectrogramWindow ? lower : upper;
		}
	}

	/// <summary>
	/// Gets the analysis hop after clamping to between 1 sample and the window length.
	/// </summary>
	internal int SpectrogramHopValue => Math.Clamp(SpectrogramHopSize, 1, SpectrogramWindowValue);

	/// <summary>
	/// Gets the decoding sample rate after clamping to the allowed range.
	/// </summary>
	internal int SpectrogramSampleRateValue => Math.Clamp(SpectrogramSampleRate, MinimumSpectrogramSampleRate, MaximumSpectrogramSampleRate);

	/// <summary>
	/// Gets the highest frequency shown after clamping between 100 Hz and half the decoding sample rate.
	/// </summary>
	internal double SpectrogramMaxFrequencyValue =>
		Math.Clamp(double.IsFinite(SpectrogramMaxFrequency) ? SpectrogramMaxFrequency : DefaultSpectrogramMaxFrequency, 100, SpectrogramSampleRateValue / 2.0);

	/// <summary>
	/// Gets the sliding window in seconds after clamping, or null when the whole media is shown.
	/// </summary>
	internal double? SlidingWindowSeconds => SpectrogramWindow is { } w && w > TimeSpan.Zero
		? Math.Clamp(w.TotalSeconds, 1, 3600)
		: null;

	/// <summary>
	/// Gets the follow margin after clamping to between 0 and 45 percent.
	/// </summary>
	internal double SpectrogramFollowMarginValue =>
		Math.Clamp(double.IsFinite(SpectrogramFollowMarginPercent) ? SpectrogramFollowMarginPercent : 15, 0, 45);

	private static string? FollowClass(bool follows) => follows ? "pdmp-follow-view" : null;

	private string VisualAccessibleName =>
		$"{AccessibleName}: {(ShowSpectrogram ? "spectrogram" : "time and frequency plot")} (time across, frequency up)"
		+ (ShowKeyPoints ? ", with key points" : string.Empty)
		+ (ShowLandmarks ? ", with landmarks" : string.Empty);

	/// <summary>
	/// Called by the player's script when the user clicks the visualisation.
	/// </summary>
	/// <param name="seconds">The time of the click, in seconds from the start of the media.</param>
	/// <param name="frequencyHz">The frequency of the click, in hertz.</param>
	/// <returns>A task that completes when the click has been handled.</returns>
	[JSInvokable]
	public Task OnVisualisationPointer(double seconds, double frequencyHz) => InvokeAsync(async () =>
	{
		if (_disposed || !double.IsFinite(seconds))
		{
			return;
		}

		var time = MediaTimeFormatter.FromSeconds(Math.Max(0, seconds));
		if (CanSeek && time is { } target)
		{
			await SeekAsync(target).ConfigureAwait(true);
		}

		await OnVisualisationClick.InvokeAsync(new PDMediaVisualisationClick(time ?? TimeSpan.Zero, frequencyHz)).ConfigureAwait(true);
	});

	/// <summary>
	/// Called by the player's script as the spectrogram is made, with <c>loading</c>, <c>ready</c>, <c>toolong</c> or <c>failed</c>.
	/// </summary>
	/// <param name="status">The state of the spectrogram.</param>
	/// <returns>A task that completes when the status has been shown.</returns>
	[JSInvokable]
	public Task OnSpectrogramStatus(string status) => InvokeAsync(() =>
	{
		if (_disposed)
		{
			return;
		}

		_visualStatus = status switch
		{
			"loading" => "Analysing the audio…",
			"toolong" => $"The spectrogram is only made for media of up to {SpectrogramMaxDuration.TotalMinutes:0.#} minutes.",
			"failed" => "The spectrogram could not be made: the audio could not be fetched or decoded (it must be same-origin or allow cross-origin access).",
			_ => null
		};
		StateHasChanged();
	});

	/// <summary>
	/// Appends columns to the live spectrogram, for a spectrum that comes from somewhere other than the media's source, such as a
	/// microphone. Set <see cref="ShowSpectrogram"/>, and usually <see cref="SpectrogramWindow"/> so that it slides. Each column is
	/// <paramref name="bins"/> bytes from 0 (quiet) to 255 (loud), spread evenly from 0 Hz up to
	/// <see cref="SpectrogramMaxFrequency"/>, and lasts <paramref name="columnDuration"/>; each batch continues where the last
	/// ended, and time starts at zero with the first column. Key points and landmarks given in the same time base line up with it.
	/// </summary>
	/// <param name="levels">The columns, one after another, each <paramref name="bins"/> bytes (a whole number of columns).</param>
	/// <param name="bins">The number of frequency values in each column, at least 2.</param>
	/// <param name="columnDuration">The time each column covers, greater than zero.</param>
	/// <returns>A task that completes when the columns have been handed to the browser.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="levels"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="bins"/> is below 2, or <paramref name="columnDuration"/> is not above zero.</exception>
	/// <exception cref="ArgumentException"><paramref name="levels"/> is not a whole number of columns.</exception>
	public async Task PushSpectrumAsync(byte[] levels, int bins, TimeSpan columnDuration)
	{
		ArgumentNullException.ThrowIfNull(levels);
		ArgumentOutOfRangeException.ThrowIfLessThan(bins, 2);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(columnDuration, TimeSpan.Zero);
		if (levels.Length % bins != 0)
		{
			throw new ArgumentException("The levels must be a whole number of columns of bins bytes.", nameof(levels));
		}

		if (!_disposed && levels.Length > 0)
		{
			await TryInvokeVoidAsync("pushSpectrum", MediaElement, levels, bins, columnDuration.TotalSeconds).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Removes everything from the live spectrogram, so that the next columns start again at time zero.
	/// </summary>
	/// <returns>A task that completes when the live spectrogram has been cleared.</returns>
	public async Task ClearSpectrumAsync()
	{
		if (!_disposed)
		{
			await TryInvokeVoidAsync("clearSpectrum", MediaElement).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Zooms the time axis of the visualisation about the middle of what is shown. A factor below 1 zooms in (0.5 shows half the time)
	/// and above 1 zooms out, down to half a second and up to everything there is.
	/// </summary>
	/// <param name="factor">The factor to scale the visible time span by, greater than zero.</param>
	/// <returns>A task that completes when the view has changed.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="factor"/> is not a finite number above zero.</exception>
	public Task ZoomAsync(double factor) => ChangeViewAsync("zoomView", "time", Positive(factor, nameof(factor)));

	/// <summary>
	/// Zooms the frequency axis of the visualisation about the middle of what is shown, with the same factor as <see cref="ZoomAsync"/>.
	/// </summary>
	/// <param name="factor">The factor to scale the visible frequency span by, greater than zero.</param>
	/// <returns>A task that completes when the view has changed.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="factor"/> is not a finite number above zero.</exception>
	public Task ZoomFrequencyAsync(double factor) => ChangeViewAsync("zoomView", "frequency", Positive(factor, nameof(factor)));

	/// <summary>
	/// Pans the time axis of the visualisation by a fraction of what is shown: positive moves later in time, negative earlier. On a
	/// sliding window, panning back stops it following the present until the view is reset.
	/// </summary>
	/// <param name="fraction">The fraction of the visible time span to move by.</param>
	/// <returns>A task that completes when the view has changed.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="fraction"/> is not a finite number.</exception>
	public Task PanAsync(double fraction) => ChangeViewAsync("panView", "time", Finite(fraction, nameof(fraction)));

	/// <summary>
	/// Pans the frequency axis of the visualisation by a fraction of what is shown: positive moves up, negative down.
	/// </summary>
	/// <param name="fraction">The fraction of the visible frequency span to move by.</param>
	/// <returns>A task that completes when the view has changed.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="fraction"/> is not a finite number.</exception>
	public Task PanFrequencyAsync(double fraction) => ChangeViewAsync("panView", "frequency", Finite(fraction, nameof(fraction)));

	/// <summary>
	/// Shows the whole of the visualisation again (all of the time, or the sliding window following the present, and the whole
	/// frequency range).
	/// </summary>
	/// <returns>A task that completes when the view has been reset.</returns>
	public async Task ResetViewAsync()
	{
		if (!_disposed && HasVisualisation)
		{
			await TryInvokeVoidAsync("resetView", MediaElement).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Resumes following the playhead after a manual pan or zoom suspended it, and brings the playhead back into view at once. It
	/// has no effect when <see cref="SpectrogramFollowMode"/> is <see cref="MediaFollowMode.Free"/>.
	/// </summary>
	/// <returns>A task that completes when following has resumed.</returns>
	public async Task FollowPlayheadAsync()
	{
		if (!_disposed && HasVisualisation)
		{
			await TryInvokeVoidAsync("followPlayhead", MediaElement).ConfigureAwait(true);
		}
	}

	private async Task ChangeViewAsync(string function, string axis, double amount)
	{
		if (!_disposed && HasVisualisation)
		{
			await TryInvokeVoidAsync(function, MediaElement, axis, amount).ConfigureAwait(true);
		}
	}

	private static double Positive(double value, string name) =>
		double.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, "must be a finite number above zero");

	private static double Finite(double value, string name) =>
		double.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(name, value, "must be a finite number");

	private async Task SyncVisualisationAsync()
	{
		if (!HasVisualisation)
		{
			if (_visualKey is not null)
			{
				_visualKey = null;
				_visualStatus = null;
				await TryInvokeVoidAsync("clearVisualisation", MediaElement).ConfigureAwait(true);
			}

			return;
		}

		var key = string.Join(
			'|',
			_src,
			ShowSpectrogram,
			ShowKeyPoints,
			ShowLandmarks,
			SpectrogramSampleRateValue,
			SpectrogramWindowValue,
			SpectrogramHopValue,
			SpectrogramMaxFrequencyValue,
			SpectrogramMaxDuration.TotalSeconds,
			ShowSpectrogramFrequencyLabels,
			SpectrogramStartTime,
			SlidingWindowSeconds,
			AllowSpectrogramZoom,
			SpectrogramFollowMode,
			SpectrogramFollowMarginValue,
			SpectrogramFollowScroll,
			SpectrogramFollowStopsAtEnd);
		if (key == _visualKey)
		{
			if (!ReferenceEquals(KeyPoints, _visualKeyPoints) || !ReferenceEquals(Landmarks, _visualLandmarks))
			{
				_visualKeyPoints = KeyPoints;
				_visualLandmarks = Landmarks;
				await SendVisualisationDataAsync(KeyPoints, Landmarks).ConfigureAwait(true);
			}

			return;
		}

		_visualKey = key;
		_visualKeyPoints = KeyPoints;
		_visualLandmarks = Landmarks;
		await TryInvokeVoidAsync("visualise", MediaElement, VisualCanvas, _objRef, BuildVisualisationOptions()).ConfigureAwait(true);
		await SendVisualisationDataAsync(KeyPoints, Landmarks).ConfigureAwait(true);
	}

	/// <summary>
	/// Replaces the key points and landmarks that are drawn without rendering the component again, for data that changes many times a
	/// second, such as a live feed (see <see cref="PushSpectrumAsync"/>). The points and lines cross to the browser as raw bytes, so
	/// thousands cost a millisecond or two. They are drawn only if <see cref="ShowKeyPoints"/> and <see cref="ShowLandmarks"/> are
	/// set, and they stay until this method is called again or the <see cref="KeyPoints"/> or <see cref="Landmarks"/> parameters change.
	/// </summary>
	/// <param name="keyPoints">The key points, or null for none.</param>
	/// <param name="landmarks">The landmarks, or null for none.</param>
	/// <returns>A task that completes when the data has been handed to the browser.</returns>
	public async Task SetVisualisationDataAsync(IReadOnlyList<PDMediaKeyPoint>? keyPoints, IReadOnlyList<PDMediaLandmark>? landmarks)
	{
		if (!_disposed && HasVisualisation)
		{
			await SendVisualisationDataAsync(keyPoints, landmarks).ConfigureAwait(true);
		}
	}

	private object BuildVisualisationOptions() => new
	{
		showSpectrogram = ShowSpectrogram,
		showKeyPoints = ShowKeyPoints,
		showLandmarks = ShowLandmarks,
		frequencyLabels = ShowSpectrogramFrequencyLabels,
		windowSeconds = SlidingWindowSeconds,
		wheelZoom = AllowSpectrogramZoom,
		followMode = SpectrogramFollowMode.ToString().ToLowerInvariant(),
		followMarginPercent = SpectrogramFollowMarginValue,
		followScroll = SpectrogramFollowScroll.ToString().ToLowerInvariant(),
		followStopsAtEnd = SpectrogramFollowStopsAtEnd,
		startSeconds = SpectrogramStartTime is { } start ? start.TimeOfDay.TotalSeconds : (double?)null,
		sampleRate = SpectrogramSampleRateValue,
		windowSize = SpectrogramWindowValue,
		hopSize = SpectrogramHopValue,
		maxFrequency = SpectrogramMaxFrequencyValue,
		maxDuration = SpectrogramMaxDuration.TotalSeconds
	};

	private async Task SendVisualisationDataAsync(IEnumerable<PDMediaKeyPoint>? keyPoints, IEnumerable<PDMediaLandmark>? landmarks)
	{
		var points = keyPoints as IReadOnlyList<PDMediaKeyPoint> ?? [.. keyPoints ?? []];
		var lines = landmarks as IReadOnlyList<PDMediaLandmark> ?? [.. landmarks ?? []];
		await TryInvokeVoidAsync(
			"setVisualisationData",
			MediaElement,
			ToBytes(ShowKeyPoints ? FlattenKeyPoints(points) : []),
			ToBytes(ShowLandmarks ? FlattenLandmarks(lines) : []),
			ShowLandmarks ? LandmarkColours(lines) : null).ConfigureAwait(true);
	}

	private static byte[] ToBytes(double[] values)
	{
		var bytes = new byte[values.Length * sizeof(double)];
		Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
		return bytes;
	}

	private static double[] FlattenKeyPoints(IReadOnlyList<PDMediaKeyPoint> points)
	{
		var values = new double[points.Count * 3];
		for (var i = 0; i < points.Count; i++)
		{
			values[i * 3] = points[i].Time.TotalSeconds;
			values[(i * 3) + 1] = points[i].FrequencyHz;
			values[(i * 3) + 2] = Math.Clamp(points[i].Weight, 0, 1);
		}

		return values;
	}

	private static double[] FlattenLandmarks(IReadOnlyList<PDMediaLandmark> lines)
	{
		var values = new double[lines.Count * 4];
		for (var i = 0; i < lines.Count; i++)
		{
			values[i * 4] = lines[i].FromTime.TotalSeconds;
			values[(i * 4) + 1] = lines[i].FromFrequencyHz;
			values[(i * 4) + 2] = lines[i].ToTime.TotalSeconds;
			values[(i * 4) + 3] = lines[i].ToFrequencyHz;
		}

		return values;
	}

	// a colour per landmark, or null when no landmark has a colour of its own (the usual case, which then costs nothing)
	private static string?[]? LandmarkColours(IReadOnlyList<PDMediaLandmark> lines) =>
		lines.Any(l => l.Color is not null) ? [.. lines.Select(l => l.Color)] : null;
}
