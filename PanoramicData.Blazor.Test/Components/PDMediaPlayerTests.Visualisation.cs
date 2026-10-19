using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the optional visualisation of <see cref="PDMediaPlayer"/>: the spectrogram, key points and landmarks.
/// </summary>
public partial class PDMediaPlayerTests
{
	private static PDMediaKeyPoint[] KeyPointSet() =>
	[
		new() { Time = TimeSpan.FromSeconds(1), FrequencyHz = 440, Weight = 0.9 },
		new() { Time = TimeSpan.FromSeconds(2), FrequencyHz = 880, Weight = 5 }
	];

	private static PDMediaLandmark[] LandmarkSet() =>
	[
		new() { FromTime = TimeSpan.FromSeconds(1), FromFrequencyHz = 440, ToTime = TimeSpan.FromSeconds(2), ToFrequencyHz = 880, Color = "red" }
	];

	/// <summary>Nothing is drawn or loaded unless a visualisation is switched on.</summary>
	[Fact]
	public void Visualisation_IsAbsentByDefault()
	{
		var cut = RenderPlayer();

		cut.FindAll(".pdmp-visual").Should().BeEmpty();
		CountOf("visualise").Should().Be(0);
	}

	/// <summary>The canvas has a clamped height and a description of what it shows.</summary>
	[Fact]
	public void Visualisation_RendersCanvasWithClampedHeightAndDescription()
	{
		var cut = RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.ShowKeyPoints, true);
			p.Add(x => x.ShowLandmarks, true);
			p.Add(x => x.SpectrogramHeight, 5000);
		});

		var canvas = cut.Find("canvas.pdmp-visual-canvas");
		canvas.GetAttribute("style").Should().Contain("height:800px");
		canvas.GetAttribute("aria-label").Should().Contain("spectrogram").And.Contain("key points").And.Contain("landmarks");
	}

	/// <summary>Without a spectrogram the canvas is described as a plot.</summary>
	[Fact]
	public void Visualisation_WithoutSpectrogram_DescribesAPlot()
	{
		var cut = RenderPlayer(p => p.Add(x => x.ShowKeyPoints, true));

		cut.Find("canvas.pdmp-visual-canvas").GetAttribute("aria-label").Should().Contain("time and frequency plot");
	}

	/// <summary>The script gets clamped options and the points and lines flattened into arrays.</summary>
	[Fact]
	public void Visualisation_PassesOptionsAndFlattenedDataToScript()
	{
		RenderPlayer(p =>
		{
			p.Add(x => x.ShowKeyPoints, true);
			p.Add(x => x.ShowLandmarks, true);
			p.Add(x => x.KeyPoints, KeyPointSet());
			p.Add(x => x.Landmarks, LandmarkSet());
			p.Add(x => x.SpectrogramWindowSize, 1000);
			p.Add(x => x.SpectrogramHopSize, 99999);
			p.Add(x => x.SpectrogramSampleRate, 1);
			p.Add(x => x.SpectrogramMaxFrequency, double.NaN);
		});

		var call = _module.Invocations.Single(i => i.Identifier == "visualise");
		call.Arguments.Should().HaveCount(4);
		var options = call.Arguments[3]!;
		T Get<T>(string name) => (T)options.GetType().GetProperty(name)!.GetValue(options)!;
		Get<double[]>("keyPoints").Should().Equal(1, 440, 0.9, 2, 880, 1);
		Get<double[]>("landmarks").Should().Equal(1, 440, 2, 880);
		Get<string?[]>("landmarkColors").Should().Equal("red");
		Get<int>("windowSize").Should().Be(1024);
		Get<int>("hopSize").Should().Be(1024);
		Get<int>("sampleRate").Should().Be(3000);
		Get<double>("maxFrequency").Should().Be(1500);
		Get<bool>("showSpectrogram").Should().BeFalse();
	}

	/// <summary>The labels-inside option reaches the script, so the plot can fill the width and line up with the seek bar.</summary>
	[Fact]
	public void Visualisation_LabelsInside_IsPassedToScript()
	{
		RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.SpectrogramLabelsInside, true);
		});

		var options = _module.Invocations.Single(i => i.Identifier == "visualise").Arguments[3]!;
		((bool)options.GetType().GetProperty("labelsInside")!.GetValue(options)!).Should().BeTrue();
	}

	/// <summary>The analysis window is rounded to a power of two within its limits.</summary>
	[Theory]
	[InlineData(64, 64)]
	[InlineData(1, 64)]
	[InlineData(700, 512)]
	[InlineData(800, 1024)]
	[InlineData(8000, 8192)]
	[InlineData(100000, 8192)]
	public void Visualisation_RoundsWindowToPowerOfTwo(int requested, int expected)
	{
		var cut = RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.SpectrogramWindowSize, requested);
		});

		cut.Instance.SpectrogramWindowValue.Should().Be(expected);
	}

	/// <summary>The script is only called again when an option or a data collection changes.</summary>
	[Fact]
	public void Visualisation_OnlyReattachesWhenSomethingChanges()
	{
		var points = KeyPointSet();
		var cut = RenderPlayer(p =>
		{
			p.Add(x => x.ShowKeyPoints, true);
			p.Add(x => x.KeyPoints, points);
		});
		cut.Render();
		CountOf("visualise").Should().Be(1);

		cut.Render(p => p.Add(x => x.KeyPoints, KeyPointSet()));
		CountOf("visualise").Should().Be(2);
	}

	/// <summary>Switching everything off clears the script and removes the canvas.</summary>
	[Fact]
	public void Visualisation_SwitchedOff_ClearsScript()
	{
		var cut = RenderPlayer(p => p.Add(x => x.ShowKeyPoints, true));

		cut.Render(p => p.Add(x => x.ShowKeyPoints, false));

		CountOf("clearVisualisation").Should().Be(1);
		cut.FindAll(".pdmp-visual").Should().BeEmpty();
	}

	/// <summary>Status reported by the script is shown to the user.</summary>
	[Theory]
	[InlineData("loading", "Analysing")]
	[InlineData("toolong", "only made for media of up to 5")]
	[InlineData("failed", "could not be made")]
	public async Task Visualisation_ShowsStatusMessages(string status, string expected)
	{
		var cut = RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true));

		await cut.InvokeAsync(() => cut.Instance.OnSpectrogramStatus(status));

		cut.Find(".pdmp-visual-status").TextContent.Should().Contain(expected);
	}

	/// <summary>The ready status removes the message.</summary>
	[Fact]
	public async Task Visualisation_ReadyClearsStatus()
	{
		var cut = RenderPlayer(p => p.Add(x => x.ShowSpectrogram, true));
		await cut.InvokeAsync(() => cut.Instance.OnSpectrogramStatus("loading"));

		await cut.InvokeAsync(() => cut.Instance.OnSpectrogramStatus("ready"));

		cut.FindAll(".pdmp-visual-status").Should().BeEmpty();
	}

	/// <summary>A click seeks to its time and raises the callback.</summary>
	[Fact]
	public async Task Visualisation_Click_SeeksAndRaisesCallback()
	{
		PDMediaVisualisationClick? clicked = null;
		var cut = await RenderReadyAsync(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.OnVisualisationClick, c => clicked = c);
		});

		await cut.InvokeAsync(() => cut.Instance.OnVisualisationPointer(12.5, 1000));

		SeekTargets().Should().Contain(12.5);
		clicked.Should().Be(new PDMediaVisualisationClick(TimeSpan.FromSeconds(12.5), 1000));
	}

	/// <summary>A click before the media is ready raises the callback but cannot seek.</summary>
	[Fact]
	public async Task Visualisation_Click_BeforeReady_StillRaisesCallbackWithoutSeeking()
	{
		PDMediaVisualisationClick? clicked = null;
		var cut = RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.OnVisualisationClick, c => clicked = c);
		});

		await cut.InvokeAsync(() => cut.Instance.OnVisualisationPointer(3, 500));

		SeekTargets().Should().BeEmpty();
		clicked.Should().NotBeNull();
	}

	/// <summary>A click with a time that is not a number is ignored.</summary>
	[Fact]
	public async Task Visualisation_Click_IgnoresNonFiniteTime()
	{
		var raised = false;
		var cut = RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.OnVisualisationClick, _ => raised = true);
		});

		await cut.InvokeAsync(() => cut.Instance.OnVisualisationPointer(double.NaN, 500));

		raised.Should().BeFalse();
	}

	/// <summary>Callbacks from the script after disposal do nothing.</summary>
	[Fact]
	public async Task Visualisation_CallbacksAfterDisposal_AreIgnored()
	{
		var raised = false;
		var cut = RenderPlayer(p =>
		{
			p.Add(x => x.ShowSpectrogram, true);
			p.Add(x => x.OnVisualisationClick, _ => raised = true);
		});
		var player = cut.Instance;
		await DisposeComponentsAsync();

		await player.OnVisualisationPointer(1, 100);
		await player.OnSpectrogramStatus("failed");

		raised.Should().BeFalse();
	}
}
