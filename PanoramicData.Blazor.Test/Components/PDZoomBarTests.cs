using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDZoomBar"/>: the zoom buttons' enabled state, stepping through the zoom steps, and
/// the JavaScript module calls that keep the canvas in step.
/// </summary>
public partial class PDZoomBarTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDZoomBar.razor.js";

	private readonly List<double> _changes = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDZoomBarTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDZoomBar> RenderZoomBar(double zoom, double[]? steps = null, string id = "zb")
	{
		var options = new ZoomBarOptions();
		if (steps is not null)
		{
			options.ZoomSteps = steps;
		}

		return Render<PDZoomBar>(p => p
			.Add(x => x.Id, id)
			.Add(x => x.Width, 300)
			.Add(x => x.Options, options)
			.Add(x => x.Value, new ZoombarValue { Zoom = zoom })
			.Add(x => x.ValueChanged, (ZoombarValue v) => _changes.Add(v.Zoom)));
	}

	private static bool IsDisabled(IRenderedComponent<PDZoomBar> cut, int index)
		=> cut.FindAll("button")[index].HasAttribute("disabled");

	/// <summary>The root and canvas carry ids derived from the component id, and the canvas has the given width.</summary>
	[Fact]
	public void Renders_RootAndCanvas_WithDerivedIds()
	{
		var cut = RenderZoomBar(100);

		cut.Find("div.pd-zoombar").Id.Should().Be("zb");
		var canvas = cut.Find("canvas");
		canvas.Id.Should().Be("zb-canvas");
		canvas.GetAttribute("width").Should().Be("300");
		canvas.GetAttribute("height").Should().Be("20");
	}

	/// <summary>At full size (the last step) zooming out is disabled and zooming in is enabled.</summary>
	[Fact]
	public void AtLastStep_ZoomOutDisabled_ZoomInEnabled()
	{
		var cut = RenderZoomBar(100);

		IsDisabled(cut, 0).Should().BeTrue();
		IsDisabled(cut, 1).Should().BeFalse();
	}

	/// <summary>At the first step zooming in is disabled and zooming out is enabled.</summary>
	[Fact]
	public void AtFirstStep_ZoomInDisabled_ZoomOutEnabled()
	{
		var cut = RenderZoomBar(10);

		IsDisabled(cut, 0).Should().BeFalse();
		IsDisabled(cut, 1).Should().BeTrue();
	}

	/// <summary>With no zoom steps both buttons are disabled.</summary>
	[Fact]
	public void NoSteps_BothButtonsDisabled()
	{
		var cut = RenderZoomBar(100, []);

		IsDisabled(cut, 0).Should().BeTrue();
		IsDisabled(cut, 1).Should().BeTrue();
	}

	/// <summary>Zooming in moves to the previous (smaller) step, tells the canvas and raises ValueChanged.</summary>
	[Fact]
	public void ZoomIn_MovesToPreviousStep_AndNotifies()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var cut = RenderZoomBar(100);

		cut.FindAll("button")[1].Click();

		_changes.Should().Equal(90);
		cut.Instance.Value.Zoom.Should().Be(90);
		module.Invocations["setValue"].Should().ContainSingle()
			.Which.Arguments[0].Should().Be("zb-canvas");
	}

	/// <summary>Zooming out moves to the next (larger) step, tells the canvas and raises ValueChanged.</summary>
	[Fact]
	public void ZoomOut_MovesToNextStep_AndNotifies()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var cut = RenderZoomBar(50);

		cut.FindAll("button")[0].Click();

		_changes.Should().Equal(60);
		module.Invocations["setValue"].Should().ContainSingle();
	}

	/// <summary>
	/// Zooming out from a zoom that is not one of the steps moves to the next larger step,
	/// not to the smallest step (#173).
	/// </summary>
	[Fact]
	public async Task ZoomOut_FromValueBetweenSteps_MovesToNextLargerStep()
	{
		var cut = RenderZoomBar(55);

		await cut.InvokeAsync(() => cut.FindAll("button")[0].ClickAsync(new MouseEventArgs()));

		_changes.Should().Equal(60);
		cut.Instance.Value.Zoom.Should().Be(60);
	}

	/// <summary>Zooming in from a zoom that is not one of the steps moves to the next smaller step (#173).</summary>
	[Fact]
	public async Task ZoomIn_FromValueBetweenSteps_MovesToNextSmallerStep()
	{
		var cut = RenderZoomBar(55);

		await cut.InvokeAsync(() => cut.FindAll("button")[1].ClickAsync(new MouseEventArgs()));

		_changes.Should().Equal(50);
		cut.Instance.Value.Zoom.Should().Be(50);
	}

	/// <summary>Zooming out from beyond the largest step does nothing, as there is no larger step.</summary>
	[Fact]
	public async Task ZoomOut_FromBeyondLastStep_DoesNothing()
	{
		var cut = RenderZoomBar(150);

		await cut.InvokeAsync(() => cut.FindAll("button")[0].ClickAsync(new MouseEventArgs()));

		_changes.Should().BeEmpty();
		cut.Instance.Value.Zoom.Should().Be(150);
	}

	/// <summary>Instances created without an explicit id get distinct generated ids.</summary>
	[Fact]
	public void DefaultIds_AreDistinct()
	{
		var first = Render<PDZoomBar>();
		var second = Render<PDZoomBar>();

		first.Instance.Id.Should().StartWith("pd-zoombar-");
		second.Instance.Id.Should().NotBe(first.Instance.Id);
	}
}
