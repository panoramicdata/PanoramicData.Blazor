using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMediaPlayerPage
{
	private const string DeadAir = "dead-air";
	private const string Clipping = "clipping";
	private const string Segment = "segment";
	private const string Generated = "generated";

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
		[Generated] = "Generated"
	};

	private readonly Dictionary<string, string> _kindColors = new()
	{
		[DeadAir] = "#dc3545",
		[Clipping] = "#fd7e14",
		[Segment] = "#0d6efd",
		[Generated] = "#6c757d"
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

	private PDMediaPlayer? _player;
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

	private void Log(string name) => EventManager?.Add(new Event(name));

	private void Log(string name, TimeSpan? position)
		=> EventManager?.Add(new Event(name, new EventArgument("Position", position is { } p ? MediaTimeFormatter.Format(p, _duration) : "live")));
}
