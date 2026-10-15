using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the keyboard shortcuts, the shaded range and error reporting of <see cref="PDMediaPlayer"/>.
/// </summary>
public partial class PDMediaPlayerTests
{
	private static Task PressAsync(IRenderedComponent<PDMediaPlayer> cut, string key, bool ctrl = false)
		=> cut.Find(".pd-media-player").KeyDownAsync(new KeyboardEventArgs { Key = key, CtrlKey = ctrl });

	/// <summary>The root advertises its shortcuts and can take focus, so that clicking inside it enables them.</summary>
	[Fact]
	public void Root_AdvertisesShortcuts()
	{
		var root = RenderPlayer().Find(".pd-media-player");

		root.GetAttribute("aria-keyshortcuts").Should().Be("K J L [ ] M < >");
		root.GetAttribute("tabindex").Should().Be("-1");
	}

	/// <summary>K plays and pauses, M mutes, and modified presses are ignored.</summary>
	[Fact]
	public async Task Shortcuts_PlayPauseAndMute()
	{
		var cut = await RenderReadyAsync();

		await PressAsync(cut, "k");
		cut.Instance.IsPlaying.Should().BeTrue();
		await PressAsync(cut, "K");
		cut.Instance.IsPlaying.Should().BeFalse();
		await PressAsync(cut, "k", ctrl: true);
		cut.Instance.IsPlaying.Should().BeFalse();

		await PressAsync(cut, "m");
		cut.Instance.IsMuted.Should().BeTrue();
		await PressAsync(cut, "x");
		cut.Instance.IsMuted.Should().BeTrue();
	}

	/// <summary>J, L, [ and ] move between markers.</summary>
	[Fact]
	public async Task Shortcuts_MoveBetweenMarkers()
	{
		var cut = await RenderReadyAsync(p => p.Add(x => x.Markers, _markers));

		await PressAsync(cut, "l");
		await PressAsync(cut, "]");
		await PressAsync(cut, "j");
		await PressAsync(cut, "[");

		SeekTargets().Should().Equal(60.0, 75.0, 60.0);
	}

	/// <summary>&lt; and &gt; step through the speed menu.</summary>
	[Fact]
	public async Task Shortcuts_ChangeTheSpeed()
	{
		var cut = await RenderReadyAsync();

		await PressAsync(cut, ">");
		await PressAsync(cut, ">");
		cut.Instance.CurrentPlaybackRate.Should().Be(1.5);
		await PressAsync(cut, "<");
		await PressAsync(cut, "<");
		await PressAsync(cut, "<");
		await PressAsync(cut, "<");
		cut.Instance.CurrentPlaybackRate.Should().Be(0.75);
	}

	/// <summary>The shaded range is drawn on the bar, and crossing into and out of it raises its events.</summary>
	[Fact]
	public async Task Range_IsShadedAndCrossingsAreRaised()
	{
		var crossings = new List<string>();
		var cut = await RenderReadyAsync(p => p
			.Add(x => x.RangeStart, TimeSpan.FromMinutes(6))
			.Add(x => x.RangeEnd, TimeSpan.FromMinutes(54))
			.Add(x => x.OnRangeEntered, t => crossings.Add($"in {t.TotalMinutes}"))
			.Add(x => x.OnRangeLeft, t => crossings.Add($"out {t.TotalMinutes}")));

		cut.Find(".pdmp-range").GetAttribute("style").Should().Be("left:10%;width:80%");

		foreach (var minutes in new[] { 1, 2, 6, 30, 54, 55, 40, 3 })
		{
			await RaiseAsync(cut, "timeupdate", State(time: minutes * 60));
		}

		crossings.Should().Equal("in 6", "out 54", "in 40", "out 3");
	}

	/// <summary>Starting inside the range counts as entering it; without a range nothing is raised.</summary>
	[Fact]
	public async Task Range_StartingInside_Enters()
	{
		var crossings = new List<string>();
		var cut = await RenderReadyAsync(p => p
			.Add(x => x.RangeStart, TimeSpan.FromMinutes(5))
			.Add(x => x.OnRangeEntered, _ => crossings.Add("in"))
			.Add(x => x.OnRangeLeft, _ => crossings.Add("out")));

		await RaiseAsync(cut, "timeupdate", State(time: 600));
		crossings.Should().Equal("in");

		cut.Render(p => p.Add(x => x.RangeStart, (TimeSpan?)null));
		await RaiseAsync(cut, "timeupdate", State(time: 0));
		cut.FindAll(".pdmp-range").Should().BeEmpty();

		// with the range set again, playback inside it enters it afresh
		cut.Render(p => p.Add(x => x.RangeStart, TimeSpan.FromMinutes(5)));
		await RaiseAsync(cut, "timeupdate", State(time: 600));
		crossings.Should().Equal("in", "in");
	}

	/// <summary>A refused source is reported with the HTTP status found by asking the server.</summary>
	[Theory]
	[InlineData(401, MediaErrorKind.Unauthorized)]
	[InlineData(403, MediaErrorKind.Forbidden)]
	[InlineData(404, MediaErrorKind.NotFound)]
	[InlineData(200, MediaErrorKind.NotSupported)]
	public async Task Error_IsClassifiedWithTheHttpStatus(int status, MediaErrorKind kind)
	{
		_module.Setup<int?>("probe", _ => true).SetResult(status);
		MediaPlayerError? raised = null;
		var cut = RenderPlayer(p => p.Add(x => x.OnError, e => raised = e));
		await cut.InvokeAsync(() => cut.Instance.PlayAsync());

		await RaiseAsync(cut, "error", new MediaElementState { ErrorCode = 4, ErrorMessage = "MEDIA_ELEMENT_ERROR" });

		_module.VerifyInvoke("probe").Arguments.Should().Equal(Src);
		raised!.Kind.Should().Be(kind);
		cut.Instance.LastError.Should().BeSameAs(raised);
		cut.Instance.IsPlaying.Should().BeFalse();
		cut.Find(".pdmp-error").GetAttribute("role").Should().Be("alert");
		cut.Find(".pdmp-error").TextContent.Should().Contain(raised.Message);
	}

	/// <summary>A decode error needs no server check; ShowErrors false keeps the message out of the player.</summary>
	[Fact]
	public async Task Error_DecodeWithoutMessage()
	{
		MediaPlayerError? raised = null;
		var cut = RenderPlayer(p => p.Add(x => x.OnError, e => raised = e).Add(x => x.ShowErrors, false));

		await RaiseAsync(cut, "error", new MediaElementState { ErrorCode = 3 });

		raised!.Kind.Should().Be(MediaErrorKind.Decode);
		CountOf("probe").Should().Be(0);
		cut.FindAll(".pdmp-error").Should().BeEmpty();
	}

	/// <summary>A failed server check leaves the browser's own classification.</summary>
	[Fact]
	public async Task Error_ProbeFails_UsesTheBrowserCode()
	{
		_module.Setup<int?>("probe", _ => true).SetException(new JSException("offline"));
		MediaPlayerError? raised = null;
		var cut = RenderPlayer(p => p.Add(x => x.OnError, e => raised = e));

		await RaiseAsync(cut, "error", new MediaElementState { ErrorCode = 2 });

		raised!.Kind.Should().Be(MediaErrorKind.Network);
	}

	/// <summary>An error for a source that has since been replaced is dropped.</summary>
	[Fact]
	public async Task Error_ForAReplacedSource_IsDropped()
	{
		var probe = _module.Setup<int?>("probe", _ => true);
		var raised = 0;
		var cut = RenderPlayer(p => p.Add(x => x.OnError, _ => raised++));

		var pending = RaiseAsync(cut, "error", new MediaElementState { ErrorCode = 4 });
		cut.Render(p => p.Add(x => x.Src, "/media/other.wav"));
		probe.SetResult(404);
		await pending;

		raised.Should().Be(0);
		cut.Instance.LastError.Should().BeNull();
	}

	/// <summary>Loading clears the error, and so does playing again.</summary>
	[Fact]
	public async Task Error_IsClearedByLoadingOrPlaying()
	{
		var cut = RenderPlayer();
		await RaiseAsync(cut, "error", new MediaElementState { ErrorCode = 3 });
		await RaiseAsync(cut, "loadedmetadata", State());
		cut.Instance.LastError.Should().BeNull();

		await RaiseAsync(cut, "error", new MediaElementState { ErrorCode = 3 });
		await cut.InvokeAsync(() => cut.Instance.PlayAsync());
		cut.Instance.LastError.Should().BeNull();
	}
}
