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
			SpectrogramMaxDuration.TotalSeconds);
		if (key == _visualKey
			&& ReferenceEquals(KeyPoints, _visualKeyPoints)
			&& ReferenceEquals(Landmarks, _visualLandmarks))
		{
			return;
		}

		_visualKey = key;
		_visualKeyPoints = KeyPoints;
		_visualLandmarks = Landmarks;
		await TryInvokeVoidAsync("visualise", MediaElement, VisualCanvas, _objRef, BuildVisualisationOptions()).ConfigureAwait(true);
	}

	private object BuildVisualisationOptions() => new
	{
		showSpectrogram = ShowSpectrogram,
		showKeyPoints = ShowKeyPoints,
		showLandmarks = ShowLandmarks,
		sampleRate = SpectrogramSampleRateValue,
		windowSize = SpectrogramWindowValue,
		hopSize = SpectrogramHopValue,
		maxFrequency = SpectrogramMaxFrequencyValue,
		maxDuration = SpectrogramMaxDuration.TotalSeconds,
		keyPoints = ShowKeyPoints ? FlattenKeyPoints() : [],
		landmarks = ShowLandmarks ? FlattenLandmarks() : [],
		landmarkColors = ShowLandmarks ? (Landmarks ?? []).Select(l => l.Color).ToArray() : []
	};

	private double[] FlattenKeyPoints() =>
		[.. (KeyPoints ?? []).SelectMany(p => new[] { p.Time.TotalSeconds, p.FrequencyHz, Math.Clamp(p.Weight, 0, 1) })];

	private double[] FlattenLandmarks() =>
		[.. (Landmarks ?? []).SelectMany(l => new[] { l.FromTime.TotalSeconds, l.FromFrequencyHz, l.ToTime.TotalSeconds, l.ToFrequencyHz })];
}
