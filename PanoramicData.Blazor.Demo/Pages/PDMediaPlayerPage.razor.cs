using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMediaPlayerPage
{
	private const string DeadAir = "dead-air";
	private const string Clipping = "clipping";
	private const string Segment = "segment";
	private const string Generated = "generated";
	private const string Song = "song";

	private const string AudioSource = "Audio (WAV, 1 minute)";

	private readonly Dictionary<string, (string? Src, MediaKind Kind)> _sources = new()
	{
		[AudioSource] = ("_content/PanoramicData.Blazor.Demo/sounds/demo-show.wav", MediaKind.Audio),
		["Video (WebM)"] = ("files/download?path=TestVideo.webm", MediaKind.Video),
		["Missing file (404)"] = ("_content/PanoramicData.Blazor.Demo/sounds/no-such-show.wav", MediaKind.Audio),
		["No source"] = (null, MediaKind.Audio)
	};

	private readonly Dictionary<string, string> _kindLabels = new()
	{
		[DeadAir] = "Dead air",
		[Clipping] = "Clipping",
		[Segment] = "Segment",
		[Generated] = "Generated",
		[Song] = "Song",
		["talk"] = "Talk",
		["ad"] = "Advert",
		["jingle"] = "Ident"
	};

	private readonly Dictionary<string, string> _kindColors = new()
	{
		[DeadAir] = "#dc3545",
		[Clipping] = "#fd7e14",
		[Segment] = "#0d6efd",
		[Generated] = "#6c757d",
		[Song] = "#20c997",
		["talk"] = "#198754",
		["ad"] = "#6f42c1",
		["jingle"] = "#d63384"
	};

	private static readonly PDMediaMarker[] _showMarkers =
	[
		new() { Id = "seg-news", Position = TimeSpan.FromSeconds(5), Kind = Segment, Label = "News", Detail = "Top of the hour" },
		new() { Id = "seg-music", Position = TimeSpan.FromSeconds(12), Kind = Segment, Label = "Music" },
		new() { Id = "dead-1", Position = TimeSpan.FromSeconds(18), Length = TimeSpan.FromSeconds(3), Kind = DeadAir, Label = "3 s of silence", Detail = "Below -60 dBFS" },
		new() { Id = "clip-1", Position = TimeSpan.FromSeconds(30), Kind = Clipping, Label = "Clipped peaks", Detail = "Full scale for 1 s" },
		new() { Id = "clip-2", Position = TimeSpan.FromSeconds(30.6), Kind = Clipping, Label = "Clipped peaks" },
		new() { Id = "seg-interview", Position = TimeSpan.FromSeconds(35), Kind = Segment, Label = "Interview" },
		new() { Id = "dead-2", Position = TimeSpan.FromSeconds(40), Length = TimeSpan.FromSeconds(2.5), Kind = DeadAir, Label = "2.5 s of silence" },
		new() { Id = "clip-3", Position = TimeSpan.FromSeconds(47), Kind = Clipping, Label = "Clipped peaks", Detail = "Full scale for 0.5 s" },
		new() { Id = "seg-weather", Position = TimeSpan.FromSeconds(50), Kind = Segment, Label = "Weather" }
	];

	private const string HandMadeSegments = "Songs, talk and transcripts";
	private const string DerivedSegments = "Derived from the segment markers";
	private const string NoSegments = "None";

	private static PDMediaTextLine Line(double seconds, string text, string? speaker = null)
		=> new() { Start = TimeSpan.FromSeconds(seconds), Text = text, Speaker = speaker };

	private static PDMediaSegment Part(string id, double start, double end, string kind, string label)
		=> new() { Id = id, Start = TimeSpan.FromSeconds(start), End = TimeSpan.FromSeconds(end), Kind = kind, Label = label };

	// the demo recording's programme, as a playout log or a speech-to-text service might describe it
	private static readonly PDMediaSegment[] _showSegments =
	[
		Part("ident-1", 0, 5, "jingle", "Station ident"),
		new()
		{
			Id = "news",
			Start = TimeSpan.FromSeconds(5),
			End = TimeSpan.FromSeconds(12),
			Kind = "talk",
			Label = "News headlines",
			Detail = "Presenter",
			Lines =
			[
				Line(5, "Good morning, here is the news.", "Presenter"),
				Line(7.5, "Markets opened higher this morning."),
				Line(10, "And now, back to the music.")
			]
		},
		Part("song-1", 12, 18, "song", "The Pentatonics: Morning Tune"),
		new()
		{
			Id = "interview",
			Start = TimeSpan.FromSeconds(21),
			End = TimeSpan.FromSeconds(30),
			Kind = "talk",
			Label = "Interview",
			Confidence = 0.92,
			Source = "speech detector",
			Lines =
			[
				Line(21, "Thanks for coming in today.", "Presenter"),
				Line(24, "It is a pleasure to be here.", "Guest"),
				Line(27, "Tell us about the new record.", "Presenter")
			]
		},
		Part("song-2", 31, 40, "song", "The Pentatonics: Second Tune"),
		new()
		{
			Id = "advert",
			Start = TimeSpan.FromSeconds(42.5),
			End = TimeSpan.FromSeconds(47),
			Kind = "ad",
			Label = "Advert",
			Text = "This programme was brought to you by the PanoramicData.Blazor demo.\nNo audio was harmed in its making."
		},
		Part("song-3", 47.5, 55, "song", "The Pentatonics: Outro"),
		Part("ident-2", 55, 60, "jingle", "Station ident")
	];

	// marker to marker: each segment marker opens a segment that runs to the next one, ignoring the fault markers
	private static readonly IReadOnlyList<PDMediaSegment> _derivedSegments = PDMediaSegment.FromMarkers(
		_showMarkers,
		TimeSpan.FromSeconds(60),
		new MediaSegmentsFromMarkersOptions { SkipKinds = [DeadAir, Clipping] });

	// set by the markup's @ref
	protected PDMediaPlayer? Player { get; set; }

	protected bool ShowSpectrogram { get; set; }

	protected bool FrequencyLabels { get; set; } = true;

	protected bool BroadcastClock { get; set; }

	protected bool SlidingWindow { get; set; }

	protected bool Controls { get; set; } = true;

	protected bool ShowKeyPoints { get; set; }

	protected bool ShowLandmarks { get; set; }

	// example data: a rising run of peaks, each joined to the next by a landmark
	private static readonly PDMediaKeyPoint[] _keyPoints =
	[
		.. Enumerable.Range(0, 24).Select(i => new PDMediaKeyPoint
		{
			Time = TimeSpan.FromSeconds(1 + (i * 2.2)),
			FrequencyHz = 300 + (i % 8 * 450),
			Weight = 0.3 + (i % 3 * 0.3)
		})
	];

	private static readonly PDMediaLandmark[] _landmarks =
	[
		.. _keyPoints.Zip(_keyPoints.Skip(1), (a, b) => new PDMediaLandmark
		{
			FromTime = a.Time,
			FromFrequencyHz = a.FrequencyHz,
			ToTime = b.Time,
			ToFrequencyHz = b.FrequencyHz
		})
	];
	private PDMediaMarker[] _markers = _showMarkers;
	private bool _manyMarkers;
	private TimeSpan? _duration;
	private string _lastMarkerText = "no marker selected";

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	protected string SourceName { get; set; } = AudioSource;

	protected MediaMarkerListPlacement Placement { get; set; } = MediaMarkerListPlacement.Below;

	protected double SeekLeadSeconds { get; set; } = 2;

	protected bool ShadeRange { get; set; } = true;

	protected bool Persist { get; set; }

	protected double Rate { get; set; } = 1;

	protected TimeSpan Position { get; set; }

	protected IReadOnlySet<string>? VisibleKinds { get; set; }

	protected string SegmentSourceName { get; set; } = HandMadeSegments;

	protected bool ManyMarkers
	{
		get => _manyMarkers;
		set
		{
			_manyMarkers = value;

			// a kind filter chosen earlier lists only the kinds it knew, so start again with every kind shown
			VisibleKinds = null;
			_markers = value ? [.. _showMarkers, .. GenerateMarkers(5000)] : _showMarkers;
		}
	}

	private string? CurrentSource => _sources[SourceName].Src;

	private MediaKind CurrentKind => _sources[SourceName].Kind;

	private IReadOnlyList<PDMediaSegment>? CurrentSegments => SegmentSourceName switch
	{
		HandMadeSegments => _showSegments,
		DerivedSegments => _derivedSegments,
		_ => null
	};

	private string PositionText => MediaTimeFormatter.Format(Position, _duration);

	// spread evenly over the minute, deliberately out of order, to show tick merging and the virtualised list
	private static IEnumerable<PDMediaMarker> GenerateMarkers(int count)
		=> Enumerable.Range(0, count)
			.Select(i => (i * 7919) % count)
			.Select(i => new PDMediaMarker
			{
				Id = $"gen-{i}",
				Position = TimeSpan.FromSeconds(60.0 * i / count),
				Kind = Generated,
				Label = $"Generated marker {i + 1}"
			});

	private void OnReady(TimeSpan? duration)
	{
		_duration = duration;
		Log("OnReady", duration);
	}

	private void OnError(MediaPlayerError error)
		=> EventManager?.Add(new Event("OnError", new EventArgument("Kind", error.Kind), new EventArgument("Message", error.Message)));

	private void OnMarkerSelected(PDMediaMarker marker)
	{
		_lastMarkerText = $"last marker: {marker.Label}";
		EventManager?.Add(new Event("OnMarkerSelected", new EventArgument("Id", marker.Id), new EventArgument("Kind", marker.Kind)));
	}

	private Task PlayInterviewAsync() => Player!.PlaySegmentAsync(_showSegments.Single(s => s.Id == "interview"));

	private void OnSegment(string name, PDMediaSegment segment)
		=> EventManager?.Add(new Event(name, new EventArgument("Id", segment.Id), new EventArgument("Label", segment.Label)));

	private void Log(string name) => EventManager?.Add(new Event(name));

	private void Log(string name, TimeSpan? position)
		=> EventManager?.Add(new Event(name, new EventArgument("Position", position is { } p ? MediaTimeFormatter.Format(p, _duration) : "live")));
}
