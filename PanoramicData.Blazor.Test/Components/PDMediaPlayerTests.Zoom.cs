using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for zooming, panning and following the playhead in the visualisation of <see cref="PDMediaPlayer"/>.
/// </summary>
public partial class PDMediaPlayerTests
{
	private IReadOnlyList<(string Axis, double Amount)> ViewCalls(string function)
		=> [.. _module.Invocations.Where(i => i.Identifier == function).Select(i => ((string)i.Arguments[1]!, Convert.ToDouble(i.Arguments[2], System.Globalization.CultureInfo.InvariantCulture)))];

	private static object Options(BunitJSModuleInterop module)
		=> module.Invocations.Last(i => i.Identifier == "visualise").Arguments[3]!;

	private static T Option<T>(object options, string name)
		=> (T)options.GetType().GetProperty(name)!.GetValue(options)!;

	/// <summary>The view buttons show with a visualisation and controls, and not otherwise or when switched off.</summary>
	[Fact]
	public void ViewControls_ShowOnlyWhenUseful()
	{
		RenderPlayer().FindAll(".pdmp-view").Should().BeEmpty();

		var shown = RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true));
		shown.FindAll(".pdmp-view").Should().HaveCount(1);
		shown.FindAll(".pdmp-follow").Should().BeEmpty();

		RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.ShowSpectrogramViewControls, false);
		}).FindAll(".pdmp-view").Should().BeEmpty();

		RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.ShowControls, false);
		}).FindAll(".pdmp-view").Should().BeEmpty();
	}

	/// <summary>Each view button asks the script for its zoom or pan, and the reset button resets.</summary>
	[Fact]
	public void ViewControls_CallTheScript()
	{
		var cut = RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true));

		cut.Find(".pdmp-zoom-in").Click();
		cut.Find(".pdmp-zoom-out").Click();
		cut.Find(".pdmp-pan-left").Click();
		cut.Find(".pdmp-pan-right").Click();
		cut.Find(".pdmp-frequency-zoom-in").Click();
		cut.Find(".pdmp-frequency-zoom-out").Click();
		cut.Find(".pdmp-frequency-up").Click();
		cut.Find(".pdmp-frequency-down").Click();
		cut.Find(".pdmp-view-reset").Click();

		ViewCalls("zoomView").Should().Equal(("time", 0.5), ("time", 2), ("frequency", 0.5), ("frequency", 2));
		ViewCalls("panView").Should().Equal(("time", -0.25), ("time", 0.25), ("frequency", 0.25), ("frequency", -0.25));
		CountOf("resetView").Should().Be(1);
	}

	/// <summary>The public view methods reach the script and refuse nonsense.</summary>
	[Fact]
	public async Task ViewMethods_ReachTheScriptAndValidate()
	{
		var cut = RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true));
		var player = cut.Instance;

		await cut.InvokeAsync(async () =>
		{
			await player.ZoomAsync(0.25);
			await player.ZoomFrequencyAsync(4);
			await player.PanAsync(-1);
			await player.PanFrequencyAsync(0.5);
			await player.FollowPlayheadAsync();
			await player.ResetViewAsync();
			await FluentActions.Awaiting(() => player.ZoomAsync(0)).Should().ThrowAsync<ArgumentOutOfRangeException>();
			await FluentActions.Awaiting(() => player.ZoomFrequencyAsync(double.NaN)).Should().ThrowAsync<ArgumentOutOfRangeException>();
			await FluentActions.Awaiting(() => player.PanAsync(double.PositiveInfinity)).Should().ThrowAsync<ArgumentOutOfRangeException>();
			await FluentActions.Awaiting(() => player.PanFrequencyAsync(double.NaN)).Should().ThrowAsync<ArgumentOutOfRangeException>();
		});

		ViewCalls("zoomView").Should().Equal(("time", 0.25), ("frequency", 4));
		ViewCalls("panView").Should().Equal(("time", -1), ("frequency", 0.5));
		CountOf("followPlayhead").Should().Be(1);
		CountOf("resetView").Should().Be(1);
	}

	/// <summary>View methods do nothing without a visualisation or after disposal.</summary>
	[Fact]
	public async Task ViewMethods_IgnoredWithoutVisualisationOrAfterDisposal()
	{
		var plain = RenderPlayer();
		await plain.InvokeAsync(async () =>
		{
			await plain.Instance.ZoomAsync(2);
			await plain.Instance.PanAsync(1);
			await plain.Instance.ResetViewAsync();
			await plain.Instance.FollowPlayheadAsync();
		});

		var shown = RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true));
		var player = shown.Instance;
		await DisposeComponentsAsync();
		await player.ZoomAsync(2);
		await player.PanAsync(1);
		await player.ResetViewAsync();
		await player.FollowPlayheadAsync();

		CountOf("zoomView").Should().Be(0);
		CountOf("panView").Should().Be(0);
		CountOf("resetView").Should().Be(0);
		CountOf("followPlayhead").Should().Be(0);
	}

	/// <summary>The wheel option and the follow settings reach the script, with the margin clamped.</summary>
	[Theory]
	[InlineData(15.0, 15.0)]
	[InlineData(-5.0, 0.0)]
	[InlineData(80.0, 45.0)]
	[InlineData(double.NaN, 15.0)]
	public void FollowSettings_ArePassedToScript(double margin, double expected)
	{
		RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.AllowSpectrogramZoom, false);
			p.Add(x => x.SpectrogramFollowMode, MediaFollowMode.Margin);
			p.Add(x => x.SpectrogramFollowMarginPercent, margin);
			p.Add(x => x.SpectrogramFollowScroll, MediaFollowScroll.PageTurn);
			p.Add(x => x.SpectrogramFollowStopsAtEnd, false);
		});

		var options = Options(_module);
		Option<bool>(options, "wheelZoom").Should().BeFalse();
		Option<string>(options, "followMode").Should().Be("margin");
		Option<double>(options, "followMarginPercent").Should().Be(expected);
		Option<string>(options, "followScroll").Should().Be("pageturn");
		Option<bool>(options, "followStopsAtEnd").Should().BeFalse();
	}

	/// <summary>By default the view is free, the wheel works and following stops at the end.</summary>
	[Fact]
	public void FollowSettings_Defaults()
	{
		RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true));

		var options = Options(_module);
		Option<bool>(options, "wheelZoom").Should().BeTrue();
		Option<string>(options, "followMode").Should().Be("free");
		Option<double>(options, "followMarginPercent").Should().Be(15);
		Option<string>(options, "followScroll").Should().Be("glide");
		Option<bool>(options, "followStopsAtEnd").Should().BeTrue();
	}

	/// <summary>The follow button shows only when a follow mode is chosen, and asks the script to follow.</summary>
	[Fact]
	public void FollowButton_ShowsWithAFollowMode()
	{
		var cut = RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.SpectrogramFollowMode, MediaFollowMode.Centre);
		});

		cut.Find(".pdmp-follow").Click();

		CountOf("followPlayhead").Should().Be(1);
	}
}
