using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDMediaPlayer"/>: rendering, the media element's events and the lifecycle. The player's
/// script is mocked with bUnit's JS interop; the script itself is checked in a real browser (see the PR).
/// </summary>
public partial class PDMediaPlayerTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDMediaPlayer.razor.js";
	private const string Src = "/media/show.wav";
	private static readonly TimeSpan _hour = TimeSpan.FromHours(1);

	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context with a loose mock of the player's script.</summary>
	public PDMediaPlayerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	private IRenderedComponent<PDMediaPlayer> RenderPlayer(Action<ComponentParameterCollectionBuilder<PDMediaPlayer>>? configure = null)
		=> Render<PDMediaPlayer>(p =>
		{
			p.Add(x => x.Src, Src);
			configure?.Invoke(p);
		});

	private static MediaElementState State(double time = 0, double? duration = 3600, bool infinite = false, double rate = 1, IReadOnlyList<double>? buffered = null)
		=> new()
		{
			CurrentTime = time,
			Duration = duration,
			IsInfinite = infinite,
			PlaybackRate = rate,
			Paused = true,
			Buffered = buffered ?? []
		};

	private static Task RaiseAsync(IRenderedComponent<PDMediaPlayer> cut, string eventName, MediaElementState? state = null)
		=> cut.InvokeAsync(() => cut.Instance.OnMediaEvent(eventName, state ?? State()));

	private async Task<IRenderedComponent<PDMediaPlayer>> RenderReadyAsync(Action<ComponentParameterCollectionBuilder<PDMediaPlayer>>? configure = null, double duration = 3600)
	{
		var cut = RenderPlayer(configure);
		await RaiseAsync(cut, "loadedmetadata", State(duration: duration));
		return cut;
	}

	private IReadOnlyList<double> SeekTargets()
		=> [.. _module.Invocations.Where(i => i.Identifier == "seek").Select(i => (double)i.Arguments[1]!)];

	private int CountOf(string identifier) => _module.Invocations.Count(i => i.Identifier == identifier);

	/// <summary>The first render imports the script and attaches it to the element with an object reference.</summary>
	[Fact]
	public void FirstRender_AttachesTheScriptToTheElement()
	{
		var cut = RenderPlayer(p => p.Add(x => x.TimeUpdateRate, 10));

		var attach = _module.VerifyInvoke("attach");
		attach.Arguments[0].Should().BeOfType<ElementReference>();
		attach.Arguments[1].Should().BeOfType<DotNetObjectReference<PDMediaPlayer>>()
			.Which.Value.Should().BeSameAs(cut.Instance);
		attach.Arguments[2].Should().Be(100);
		_module.VerifyInvoke("setRate").Arguments[1].Should().Be(1.0);
		_module.VerifyInvoke("setVolume").Arguments.Skip(1).Should().Equal(1.0, false);
	}

	/// <summary>An audio player renders a hidden audio element and an accessible region.</summary>
	[Fact]
	public void Render_Audio()
	{
		var cut = RenderPlayer(p => p.Add(x => x.Title, "Breakfast").Add(x => x.Id, "p1").Add(x => x.CssClass, "extra"));

		var root = cut.Find("#p1");
		root.GetAttribute("role").Should().Be("region");
		root.GetAttribute("aria-label").Should().Be("Breakfast");
		root.ClassList.Should().Contain(["pd-media-player", "pdmp-below", "pdmp-audio-player", "extra"]);
		cut.Find(".pdmp-title").TextContent.Should().Be("Breakfast");
		var audio = cut.Find("audio");
		audio.GetAttribute("src").Should().Be(Src);
		audio.GetAttribute("preload").Should().Be("metadata");
		audio.HasAttribute("autoplay").Should().BeFalse();
		audio.HasAttribute("loop").Should().BeFalse();
		cut.FindAll("video").Should().BeEmpty();
	}

	/// <summary>A video player renders a video element with its poster, and clicking it toggles playback.</summary>
	[Fact]
	public async Task Render_Video()
	{
		var cut = RenderPlayer(p => p
			.Add(x => x.MediaKind, MediaKind.Video)
			.Add(x => x.Poster, "/poster.png")
			.Add(x => x.Preload, MediaPreload.Auto)
			.Add(x => x.Autoplay, true)
			.Add(x => x.Loop, true)
			.Add(x => x.Muted, true));

		var video = cut.Find("video");
		video.GetAttribute("poster").Should().Be("/poster.png");
		video.GetAttribute("preload").Should().Be("auto");
		video.HasAttribute("playsinline").Should().BeTrue();
		video.HasAttribute("autoplay").Should().BeTrue();
		video.HasAttribute("loop").Should().BeTrue();
		video.HasAttribute("muted").Should().BeTrue();
		cut.Find(".pd-media-player").ClassList.Should().Contain("pdmp-video-player");
		cut.Find(".pd-media-player").GetAttribute("aria-label").Should().Be("Media player");

		await video.ClickAsync(new());

		_module.VerifyInvoke("play");
	}

	/// <summary>Preload none renders as the attribute value none.</summary>
	[Fact]
	public void Render_PreloadNone()
		=> RenderPlayer(p => p.Add(x => x.Preload, MediaPreload.None)).Find("audio").GetAttribute("preload").Should().Be("none");

	/// <summary>Before the media loads, seeking is disabled and the duration is unknown.</summary>
	[Fact]
	public void BeforeReady_SeekingIsDisabled()
	{
		var cut = RenderPlayer();

		cut.Find("input.pdmp-seek").HasAttribute("disabled").Should().BeTrue();
		cut.Find("button.pdmp-skip-back").HasAttribute("disabled").Should().BeTrue();
		cut.Find("button.pdmp-skip-forward").HasAttribute("disabled").Should().BeTrue();
		cut.Find("button.pdmp-play").HasAttribute("disabled").Should().BeFalse();
		cut.Find(".pdmp-duration").TextContent.Should().Be("-:--");
		cut.Instance.IsReady.Should().BeFalse();
		cut.Instance.Duration.Should().BeNull();
	}

	/// <summary>Without a source the element has no src and nothing can be played.</summary>
	[Fact]
	public async Task NoSource_CannotPlay()
	{
		var cut = Render<PDMediaPlayer>();

		await cut.InvokeAsync(() => cut.Instance.PlayAsync());

		cut.Find("audio").HasAttribute("src").Should().BeFalse();
		cut.Find("button.pdmp-play").HasAttribute("disabled").Should().BeTrue();
		CountOf("play").Should().Be(0);
	}

	/// <summary>Once the duration is known the player is ready: the slider is enabled and OnReady reports the duration.</summary>
	[Fact]
	public async Task Metadata_MakesThePlayerReady()
	{
		TimeSpan? ready = TimeSpan.MinValue;
		var cut = RenderPlayer(p => p.Add(x => x.OnReady, d => ready = d));

		await RaiseAsync(cut, "loadedmetadata", State(duration: 3600));

		ready.Should().Be(_hour);
		cut.Instance.IsReady.Should().BeTrue();
		cut.Instance.Duration.Should().Be(_hour);
		var slider = cut.Find("input.pdmp-seek");
		slider.HasAttribute("disabled").Should().BeFalse();
		slider.GetAttribute("max").Should().Be("3600");
		slider.GetAttribute("step").Should().Be("1");
		slider.GetAttribute("aria-valuetext").Should().Be("0 seconds of 1 hour");
		cut.Find(".pdmp-duration").TextContent.Should().Be("1:00:00");
		cut.Find(".pdmp-position").TextContent.Should().Be("0:00:00");
	}

	/// <summary>Media shorter than a minute seeks in tenths of a second.</summary>
	[Fact]
	public async Task ShortMedia_UsesAFineSeekStep()
	{
		var cut = await RenderReadyAsync(duration: 30);

		cut.Find("input.pdmp-seek").GetAttribute("step").Should().Be("0.1");
		cut.Find(".pdmp-duration").TextContent.Should().Be("0:30");
	}

	/// <summary>An unknown (not a number) duration leaves the player waiting until a later event reports one.</summary>
	[Fact]
	public async Task UnknownDuration_WaitsForADurationChange()
	{
		var readyCount = 0;
		var cut = RenderPlayer(p => p.Add(x => x.OnReady, _ => readyCount++));

		await RaiseAsync(cut, "loadedmetadata", State(duration: null));
		cut.Instance.IsReady.Should().BeFalse();

		await RaiseAsync(cut, "durationchange", State(duration: 120));
		await RaiseAsync(cut, "durationchange", State(duration: 125));

		cut.Instance.IsReady.Should().BeTrue();
		cut.Instance.Duration.Should().Be(TimeSpan.FromSeconds(125));
		readyCount.Should().Be(1);
	}

	/// <summary>A live stream (infinite duration) is ready but cannot be sought, and OnReady reports null.</summary>
	[Fact]
	public async Task LiveStream_DisablesSeeking()
	{
		TimeSpan? ready = TimeSpan.MinValue;
		var cut = RenderPlayer(p => p
			.Add(x => x.OnReady, d => ready = d)
			.Add(x => x.StartAt, TimeSpan.FromMinutes(5)));

		await RaiseAsync(cut, "loadedmetadata", State(time: 3, duration: null, infinite: true));

		ready.Should().BeNull();
		cut.Instance.IsLive.Should().BeTrue();
		cut.Instance.IsReady.Should().BeTrue();
		cut.Find("input.pdmp-seek").HasAttribute("disabled").Should().BeTrue();
		cut.Find("button.pdmp-skip-forward").HasAttribute("disabled").Should().BeTrue();
		cut.Find(".pdmp-duration").TextContent.Should().Be("Live");
		cut.Find("input.pdmp-seek").GetAttribute("aria-valuetext").Should().Be("3 seconds");
		SeekTargets().Should().BeEmpty();
	}

	/// <summary>Time updates move the position, raise OnTimeUpdate and PositionChanged, and describe it for screen readers.</summary>
	[Fact]
	public async Task TimeUpdate_MovesThePosition()
	{
		var updates = new List<TimeSpan>();
		var changes = new List<TimeSpan>();
		var cut = await RenderReadyAsync(p => p
			.Add(x => x.OnTimeUpdate, t => updates.Add(t))
			.Add(x => x.PositionChanged, t => changes.Add(t)));

		await RaiseAsync(cut, "timeupdate", State(time: 61.7));
		await RaiseAsync(cut, "seeked", State(time: 1800));

		updates.Should().Equal(TimeSpan.FromSeconds(61.7), TimeSpan.FromSeconds(1800));
		changes.Should().Equal(updates);
		cut.Instance.CurrentPosition.Should().Be(TimeSpan.FromSeconds(1800));
		cut.Find(".pdmp-position").TextContent.Should().Be("0:30:00");
		cut.Find(".pdmp-progress").GetAttribute("style").Should().Be("width:50%");
		cut.Find("input.pdmp-seek").GetAttribute("aria-valuetext").Should().Be("30 minutes of 1 hour");
	}

	/// <summary>Progress events draw the buffered ranges without raising time updates.</summary>
	[Fact]
	public async Task Progress_DrawsBufferedRanges()
	{
		var updates = 0;
		var cut = await RenderReadyAsync(p => p.Add(x => x.OnTimeUpdate, _ => updates++));

		await RaiseAsync(cut, "progress", State(buffered: [0, 360, 1800, 2160]));

		cut.FindAll(".pdmp-buffered").Select(e => e.GetAttribute("style")).Should().Equal("left:0%;width:10%", "left:50%;width:10%");
		updates.Should().Be(0);
	}

	/// <summary>Play, pause and ended events update the state and raise their callbacks.</summary>
	[Fact]
	public async Task PlayPauseEnded_RaiseTheirEvents()
	{
		var raised = new List<string>();
		var cut = await RenderReadyAsync(p => p
			.Add(x => x.OnPlay, () => raised.Add("play"))
			.Add(x => x.OnPause, () => raised.Add("pause"))
			.Add(x => x.OnEnded, () => raised.Add("ended")));

		await RaiseAsync(cut, "play");
		cut.Instance.IsPlaying.Should().BeTrue();
		cut.Find("button.pdmp-play").GetAttribute("aria-label").Should().Be("Pause");
		cut.Find(".pd-media-player").ClassList.Should().Contain("pdmp-playing");

		await RaiseAsync(cut, "pause", State(time: 12));
		cut.Instance.IsPlaying.Should().BeFalse();
		cut.Instance.CurrentPosition.Should().Be(TimeSpan.FromSeconds(12));
		cut.Find("button.pdmp-play").GetAttribute("aria-label").Should().Be("Play");

		await RaiseAsync(cut, "play");
		await RaiseAsync(cut, "ended", State(time: 3600));
		cut.Instance.IsPlaying.Should().BeFalse();

		raised.Should().Equal("play", "pause", "play", "ended");
	}

	/// <summary>Events the player does not use, and events without a snapshot, are ignored.</summary>
	[Fact]
	public async Task UnknownOrEmptyEvents_AreIgnored()
	{
		var cut = await RenderReadyAsync();

		await RaiseAsync(cut, "volumechange", State(time: 99));
		await cut.InvokeAsync(() => cut.Instance.OnMediaEvent("timeupdate", null!));

		cut.Instance.CurrentPosition.Should().Be(TimeSpan.Zero);
	}

	/// <summary>Changing the source resets the position, duration and error, and loads the new source.</summary>
	[Fact]
	public async Task SourceChange_ResetsTheState()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.StartAt, TimeSpan.FromSeconds(30)));
		await RaiseAsync(cut, "timeupdate", State(time: 600));
		await RaiseAsync(cut, "play");
		await RaiseAsync(cut, "error", new MediaElementState { ErrorCode = 3 });

		cut.Render(p => p.Add(x => x.Src, "/media/other.wav"));

		cut.Instance.CurrentPosition.Should().Be(TimeSpan.Zero);
		cut.Instance.Duration.Should().BeNull();
		cut.Instance.IsReady.Should().BeFalse();
		cut.Instance.IsPlaying.Should().BeFalse();
		cut.Instance.LastError.Should().BeNull();
		cut.Find("audio").GetAttribute("src").Should().Be("/media/other.wav");

		await RaiseAsync(cut, "loadedmetadata", State(duration: 100));
		SeekTargets().Should().Equal(30.0, 30.0);
	}

	/// <summary>Switching between audio and video renders the other element and attaches the script to it.</summary>
	[Fact]
	public void KindChange_AttachesTheNewElement()
	{
		var cut = RenderPlayer();

		cut.Render(p => p.Add(x => x.MediaKind, MediaKind.Video));

		cut.FindAll("video").Should().ContainSingle();
		cut.FindAll("audio").Should().BeEmpty();
		CountOf("attach").Should().Be(2);
	}

	/// <summary>The download button links to the download URL, or to the source, with the suggested file name.</summary>
	[Fact]
	public void DownloadButton_LinksToTheHostsUrl()
	{
		var cut = RenderPlayer(p => p.Add(x => x.ShowDownload, true).Add(x => x.DownloadFileName, "show.wav"));

		var link = cut.Find("a.pdmp-download");
		link.GetAttribute("href").Should().Be(Src);
		link.GetAttribute("download").Should().Be("show.wav");

		cut.Render(p => p.Add(x => x.DownloadUrl, "/api/download/1"));
		cut.Find("a.pdmp-download").GetAttribute("href").Should().Be("/api/download/1");

		cut.Render(p => p.Add(x => x.ShowDownload, false));
		cut.FindAll("a.pdmp-download").Should().BeEmpty();
	}

	/// <summary>Without a script (the import failed), every method is still safe to call.</summary>
	[Fact]
	public async Task WithoutTheScript_MethodsAreSafe()
	{
		// a separate context with no script set up, in strict mode, makes the import fail
		using var context = new BunitContext();
		context.JSInterop.Mode = JSRuntimeMode.Strict;
		var cut = context.Render<PDMediaPlayer>(p => p.Add(x => x.Src, Src));

		await cut.InvokeAsync(() => cut.Instance.PlayAsync());
		cut.Find("button.pdmp-play").GetAttribute("aria-label").Should().Be("Play");
		await cut.InvokeAsync(() => cut.Instance.TogglePlayAsync());
		await cut.InvokeAsync(() => cut.Instance.TogglePlayAsync());
		await cut.InvokeAsync(() => cut.Instance.SeekAsync(TimeSpan.FromSeconds(5)));
		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(2));
		await cut.InvokeAsync(() => cut.Instance.SetVolumeAsync(0.5));
		await cut.InvokeAsync(() => cut.Instance.ToggleMuteAsync());
		await cut.InvokeAsync(() => cut.Instance.NextMarkerAsync());

		cut.Instance.CurrentPosition.Should().Be(TimeSpan.FromSeconds(5));
		cut.Instance.CurrentPlaybackRate.Should().Be(2);
		cut.Instance.CurrentVolume.Should().Be(0.5);
		cut.Instance.IsMuted.Should().BeTrue();

		// an error reported without the script cannot ask the server, so the browser's code decides
		await cut.InvokeAsync(() => cut.Instance.OnMediaEvent("error", new MediaElementState { ErrorCode = 4 }));
		cut.Instance.LastError!.Kind.Should().Be(MediaErrorKind.NotSupported);
		await cut.Instance.DisposeAsync();
	}
}
