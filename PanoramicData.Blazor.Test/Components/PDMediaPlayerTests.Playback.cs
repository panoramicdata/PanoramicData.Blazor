using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the playback methods of <see cref="PDMediaPlayer"/>: play, pause, seek, skip, speed and volume.
/// </summary>
public partial class PDMediaPlayerTests
{
	/// <summary>Play and pause call the script, and toggling alternates between them.</summary>
	[Fact]
	public async Task PlayPauseToggle_CallTheScript()
	{
		var cut = await RenderReadyAsync();

		await cut.Find("button.pdmp-play").ClickAsync(new MouseEventArgs());
		cut.Instance.IsPlaying.Should().BeTrue();
		CountOf("play").Should().Be(1);

		await cut.InvokeAsync(() => cut.Instance.TogglePlayAsync());
		cut.Instance.IsPlaying.Should().BeFalse();
		CountOf("pause").Should().Be(1);

		await cut.InvokeAsync(() => cut.Instance.TogglePlayAsync());
		CountOf("play").Should().Be(2);
		await cut.InvokeAsync(() => cut.Instance.PauseAsync());
		CountOf("pause").Should().Be(2);
	}

	/// <summary>When the browser blocks playback, the player stays paused.</summary>
	[Fact]
	public async Task Play_Blocked_StaysPaused()
	{
		_module.Setup<string?>("play", _ => true).SetResult("NotAllowedError");
		var cut = await RenderReadyAsync();

		await cut.InvokeAsync(() => cut.Instance.PlayAsync());

		cut.Instance.IsPlaying.Should().BeFalse();
	}

	/// <summary>When the play call fails in the script, the player stays paused.</summary>
	[Fact]
	public async Task Play_ScriptFails_StaysPaused()
	{
		_module.Setup<string?>("play", _ => true).SetException(new JSException("element gone"));
		var cut = await RenderReadyAsync();

		await cut.InvokeAsync(() => cut.Instance.PlayAsync());

		cut.Instance.IsPlaying.Should().BeFalse();
	}

	/// <summary>A play request made before the script has loaded is carried out once it has.</summary>
	[Fact]
	public void Play_BeforeTheScriptLoads_StartsOnceLoaded()
	{
		var host = Render<EarlyPlayHost>();

		host.Instance.Requested.Should().BeTrue();
		CountOf("play").Should().Be(1);
		_module.Invocations.Select(i => i.Identifier).Should().ContainInOrder("attach", "play");
	}

	/// <summary>A seek requested before the media is ready waits for it, and wins over StartAt.</summary>
	[Fact]
	public async Task Seek_BeforeReady_WaitsForTheMedia()
	{
		var cut = RenderPlayer(p => p.Add(x => x.StartAt, TimeSpan.FromSeconds(30)));

		await cut.InvokeAsync(() => cut.Instance.SeekAsync(TimeSpan.FromMinutes(10)));
		await cut.InvokeAsync(() => cut.Instance.SkipAsync(TimeSpan.FromMinutes(1)));

		SeekTargets().Should().BeEmpty();
		cut.Instance.CurrentPosition.Should().Be(TimeSpan.FromMinutes(11));
		cut.Find(".pdmp-position").TextContent.Should().Be("11:00");

		await RaiseAsync(cut, "loadedmetadata", State(duration: 3600));

		SeekTargets().Should().Equal(660.0);
	}

	/// <summary>StartAt is applied when the media loads.</summary>
	[Fact]
	public async Task StartAt_IsAppliedWhenReady()
	{
		await RenderReadyAsync(p => p.Add(x => x.StartAt, TimeSpan.FromMinutes(5)));

		SeekTargets().Should().Equal(300.0);
	}

	/// <summary>Seeks are limited to the media, and every seek raises PositionChanged.</summary>
	[Fact]
	public async Task Seek_IsLimitedToTheMedia()
	{
		var changes = new List<TimeSpan>();
		var cut = await RenderReadyAsync(p => p.Add(x => x.PositionChanged, t => changes.Add(t)));

		await cut.InvokeAsync(() => cut.Instance.SeekAsync(TimeSpan.FromHours(2)));
		await cut.InvokeAsync(() => cut.Instance.SeekAsync(TimeSpan.FromSeconds(-5)));
		await cut.InvokeAsync(() => cut.Instance.SeekAsync(TimeSpan.FromSeconds(100)));

		SeekTargets().Should().Equal(3600.0, 0.0, 100.0);
		changes.Should().Equal(_hour, TimeSpan.Zero, TimeSpan.FromSeconds(100));
	}

	/// <summary>The skip buttons move back and forward by their configured distances.</summary>
	[Fact]
	public async Task SkipButtons_MoveByTheirDistances()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.SkipBackSeconds, 10).Add(x => x.SkipForwardSeconds, 45));
		await RaiseAsync(cut, "timeupdate", State(time: 100));

		cut.Find("button.pdmp-skip-back").GetAttribute("aria-label").Should().Be("Back 10 seconds");
		cut.Find("button.pdmp-skip-forward").GetAttribute("aria-label").Should().Be("Forward 45 seconds");
		await cut.Find("button.pdmp-skip-forward").ClickAsync(new MouseEventArgs());
		await cut.Find("button.pdmp-skip-back").ClickAsync(new MouseEventArgs());

		SeekTargets().Should().Equal(145.0, 135.0);
	}

	/// <summary>The default skip distances are 15 seconds back and 30 forward.</summary>
	[Fact]
	public void SkipButtons_Defaults()
	{
		var cut = RenderPlayer();

		cut.Find("button.pdmp-skip-back .pdmp-skip-label").TextContent.Should().Be("15");
		cut.Find("button.pdmp-skip-forward .pdmp-skip-label").TextContent.Should().Be("30");
	}

	/// <summary>Dragging the slider shows the time being dragged to, and releasing it seeks there.</summary>
	[Fact]
	public async Task SeekSlider_ScrubsThenSeeks()
	{
		var cut = await RenderReadyAsync();
		var slider = cut.Find("input.pdmp-seek");

		await slider.InputAsync(new ChangeEventArgs { Value = "1200" });
		cut.Find(".pdmp-position").TextContent.Should().Be("0:20:00");
		await RaiseAsync(cut, "timeupdate", State(time: 5));
		cut.Find(".pdmp-position").TextContent.Should().Be("0:20:00");
		SeekTargets().Should().BeEmpty();

		await cut.Find("input.pdmp-seek").ChangeAsync(new ChangeEventArgs { Value = "1200.5" });
		SeekTargets().Should().Equal(1200.5);

		await cut.Find("input.pdmp-seek").InputAsync(new ChangeEventArgs { Value = "not a number" });
		await cut.Find("input.pdmp-seek").ChangeAsync(new ChangeEventArgs { Value = null });
		SeekTargets().Should().ContainSingle();
	}

	/// <summary>Setting the speed limits it, applies it, raises PlaybackRateChanged once, and keeps it for the next source.</summary>
	[Fact]
	public async Task SetPlaybackRate_AppliesAndKeepsTheSpeed()
	{
		var rates = new List<double>();
		var cut = await RenderReadyAsync(p => p.Add(x => x.PlaybackRateChanged, r => rates.Add(r)));

		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(1.5));
		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(1.5));
		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(10));
		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(double.NaN));

		rates.Should().Equal(1.5, 4, 1);
		cut.Instance.CurrentPlaybackRate.Should().Be(1);

		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(1.75));
		cut.Render(p => p.Add(x => x.Src, "/media/next.wav"));
		cut.Instance.CurrentPlaybackRate.Should().Be(1.75);

		// the new source starts at the browser's own rate, so the chosen one is applied again
		var setRatesBefore = CountOf("setRate");
		await RaiseAsync(cut, "loadedmetadata", State(duration: 60, rate: 1));
		CountOf("setRate").Should().Be(setRatesBefore + 1);
		_module.Invocations.Last(i => i.Identifier == "setRate").Arguments[1].Should().Be(1.75);
	}

	/// <summary>The speed menu lists the rates and changing it sets the speed; a speed outside the list is added.</summary>
	[Fact]
	public async Task SpeedMenu_ListsAndSetsTheRate()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.PlaybackRates, [2, 1, 0.1, 1]));

		cut.FindAll("select.pdmp-rate option").Select(o => o.TextContent).Should().Equal("1×", "2×");
		cut.Instance.AvailablePlaybackRates.Should().Equal(1, 2);

		await cut.Find("select.pdmp-rate").ChangeAsync(new ChangeEventArgs { Value = "2" });
		cut.Instance.CurrentPlaybackRate.Should().Be(2);
		cut.Find("select.pdmp-rate").GetAttribute("value").Should().Be("2");

		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(3.5));
		cut.FindAll("select.pdmp-rate option").Select(o => o.TextContent).Should().Equal("3.5×", "1×", "2×");

		await cut.Find("select.pdmp-rate").ChangeAsync(new ChangeEventArgs { Value = "fast" });
		cut.Instance.CurrentPlaybackRate.Should().Be(3.5);
	}

	/// <summary>A new PlaybackRate parameter is applied; passing the same value again changes nothing.</summary>
	[Fact]
	public async Task PlaybackRateParameter_IsApplied()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.PlaybackRate, 1.25));
		cut.Instance.CurrentPlaybackRate.Should().Be(1.25);

		cut.Render(p => p.Add(x => x.PlaybackRate, 2));
		cut.Instance.CurrentPlaybackRate.Should().Be(2);

		await cut.InvokeAsync(() => cut.Instance.SetPlaybackRateAsync(1.5));
		cut.Render(p => p.Add(x => x.PlaybackRate, 2));
		cut.Instance.CurrentPlaybackRate.Should().Be(1.5);
	}

	/// <summary>Volume and mute are applied together, raise their changed callbacks and follow their parameters.</summary>
	[Fact]
	public async Task VolumeAndMute_AreApplied()
	{
		var volumes = new List<double>();
		var mutes = new List<bool>();
		var cut = await RenderReadyAsync(p => p
			.Add(x => x.VolumeChanged, v => volumes.Add(v))
			.Add(x => x.MutedChanged, m => mutes.Add(m)));

		await cut.Find("input.pdmp-volume-slider").InputAsync(new ChangeEventArgs { Value = "0.25" });
		cut.Find("input.pdmp-volume-slider").GetAttribute("aria-valuetext").Should().Be("25%");
		await cut.Find("input.pdmp-volume-slider").InputAsync(new ChangeEventArgs { Value = "x" });
		await cut.InvokeAsync(() => cut.Instance.SetVolumeAsync(7));
		await cut.InvokeAsync(() => cut.Instance.SetVolumeAsync(double.NaN));
		await cut.InvokeAsync(() => cut.Instance.SetVolumeAsync(1));
		volumes.Should().Equal(0.25, 1);

		await cut.Find("button.pdmp-mute").ClickAsync(new MouseEventArgs());
		cut.Find("button.pdmp-mute").GetAttribute("aria-pressed").Should().Be("true");
		cut.Find("button.pdmp-mute i").ClassList.Should().Contain("fa-volume-mute");
		await cut.InvokeAsync(() => cut.Instance.SetMutedAsync(true));
		mutes.Should().Equal(true);
		_module.Invocations.Last(i => i.Identifier == "setVolume").Arguments.Skip(1).Should().Equal(1.0, true);

		// the host passing the value it passed before is not a change; a new value is
		cut.Render(p => p.Add(x => x.Muted, false).Add(x => x.Volume, 1));
		cut.Instance.IsMuted.Should().BeTrue();
		cut.Render(p => p.Add(x => x.Muted, true));
		cut.Render(p => p.Add(x => x.Muted, false).Add(x => x.Volume, 0.5));
		cut.Instance.IsMuted.Should().BeFalse();
		cut.Instance.CurrentVolume.Should().Be(0.5);
	}

	/// <summary>A bound host that passes back older reported positions late does not cause seeks.</summary>
	[Fact]
	public async Task PositionParameter_LateEchoes_DoNotSeek()
	{
		var cut = await RenderReadyAsync();
		for (var second = 1; second <= 12; second++)
		{
			await RaiseAsync(cut, "timeupdate", State(time: second));
		}

		cut.Render(p => p.Add(x => x.Position, TimeSpan.FromSeconds(9)));
		SeekTargets().Should().BeEmpty();

		// a position reported long ago is a genuine request
		cut.Render(p => p.Add(x => x.Position, TimeSpan.FromSeconds(2)));
		SeekTargets().Should().Equal(2.0);
	}

	/// <summary>A host that does not bind Position can re-render without sending the player back to where it started.</summary>
	[Fact]
	public async Task PositionParameter_Unbound_DoesNotPullThePlayerBack()
	{
		var cut = await RenderReadyAsync();
		await RaiseAsync(cut, "timeupdate", State(time: 300));

		cut.Render(p => p.Add(x => x.Title, "Re-rendered"));

		SeekTargets().Should().BeEmpty();
		cut.Instance.CurrentPosition.Should().Be(TimeSpan.FromSeconds(300));
	}

	/// <summary>A new Position parameter seeks; the echo of a position the player reported does not.</summary>
	[Fact]
	public async Task PositionParameter_Seeks()
	{
		var cut = await RenderReadyAsync();

		cut.Render(p => p.Add(x => x.Position, TimeSpan.FromSeconds(90)));
		SeekTargets().Should().Equal(90.0);

		await RaiseAsync(cut, "timeupdate", State(time: 95));
		cut.Render(p => p.Add(x => x.Position, TimeSpan.FromSeconds(95)));
		SeekTargets().Should().ContainSingle();
	}

	/// <summary>Changing TimeUpdateRate reconfigures the script.</summary>
	[Fact]
	public void TimeUpdateRate_ReconfiguresTheScript()
	{
		var cut = RenderPlayer();

		cut.Render(p => p.Add(x => x.TimeUpdateRate, 10));
		cut.Render(p => p.Add(x => x.TimeUpdateRate, 10));

		_module.Invocations.Where(i => i.Identifier == "configure").Should().ContainSingle()
			.Which.Arguments[1].Should().Be(100);
	}

	/// <summary>A component that asks its player to play from its own first after-render, before the player's script loads.</summary>
	private sealed class EarlyPlayHost : ComponentBase
	{
		private PDMediaPlayer? _player;

		public bool Requested { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<PDMediaPlayer>(0);
			builder.AddAttribute(1, nameof(PDMediaPlayer.Src), Src);
			builder.AddComponentReferenceCapture(2, r => _player = (PDMediaPlayer)r);
			builder.CloseComponent();
		}

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender && _player is not null)
			{
				Requested = true;
				await _player.PlayAsync();
			}
		}
	}
}
