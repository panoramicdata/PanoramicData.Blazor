namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMediaPlayerDocumentation
{
	private const string ShowUrl = "_content/PanoramicData.Blazor.Demo/sounds/demo-show.wav";

	private readonly PDMediaMarker[] _markers =
	[
		new() { Id = "1", Position = TimeSpan.FromSeconds(18), Length = TimeSpan.FromSeconds(3), Kind = "dead-air", Label = "Silence" },
		new() { Id = "2", Position = TimeSpan.FromSeconds(30), Kind = "clipping", Label = "Clipped peaks" },
		new() { Id = "3", Position = TimeSpan.FromSeconds(40), Length = TimeSpan.FromSeconds(2.5), Kind = "dead-air", Label = "Silence" },
		new() { Id = "4", Position = TimeSpan.FromSeconds(47), Kind = "clipping", Label = "Clipped peaks" }
	];

	private readonly Dictionary<string, string> _kindLabels = new()
	{
		["dead-air"] = "Dead air",
		["clipping"] = "Clipping"
	};

	private const string _basicCode = """
		<PDMediaPlayer Src="/recordings/show.wav"
		               Title="The breakfast show" />
		""";

	private const string _markersCode = """
		<PDMediaPlayer Src="/recordings/show.wav"
		               Markers="_markers"
		               KindLabels="_kindLabels"
		               SeekLead="TimeSpan.FromSeconds(3)"
		               RangeStart="TimeSpan.FromSeconds(5)"
		               RangeEnd="TimeSpan.FromSeconds(55)"
		               @bind-PlaybackRate="_rate"
		               OnMarkerSelected="m => _selected = m" />

		@code {
		    private double _rate = 1;
		    private PDMediaMarker? _selected;

		    private readonly PDMediaMarker[] _markers =
		    [
		        new() { Id = "1", Position = TimeSpan.FromSeconds(18), Length = TimeSpan.FromSeconds(3), Kind = "dead-air", Label = "Silence" },
		        new() { Id = "2", Position = TimeSpan.FromSeconds(30), Kind = "clipping", Label = "Clipped peaks" }
		    ];

		    private readonly Dictionary<string, string> _kindLabels = new()
		    {
		        ["dead-air"] = "Dead air",
		        ["clipping"] = "Clipping"
		    };
		}
		""";

	private const string _apiCode = """
		<PDMediaPlayer @ref="_player" Src="@_src" Markers="_markers" PersistKey="archive-player" />

		<button @onclick="() => _player!.NextMarkerAsync("dead-air")">Next dead air</button>
		<button @onclick="() => _player!.SeekAsync(TimeSpan.FromMinutes(5))">Skip the intro</button>

		@code {
		    private PDMediaPlayer? _player;
		}
		""";

	private double Rate { get; set; } = 1;
	private PDMediaMarker? Selected { get; set; }
}
