using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDZoomBar"/>: the zoom buttons' enabled state, stepping through the zoom steps, and
/// the JavaScript module calls that keep the canvas in step.
/// </summary>
public class PDZoomBarTests : BunitContext
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

	/// <summary>On first render the module is initialised with the canvas id, value and options.</summary>
	[Fact]
	public void FirstRender_InitialisesTheModule()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var cut = RenderZoomBar(40);

		var init = module.Invocations["initialize"].Should().ContainSingle().Subject;
		init.Arguments[0].Should().Be("zb-canvas");
		init.Arguments[1].Should().BeSameAs(cut.Instance.Value);
		init.Arguments[2].Should().BeSameAs(cut.Instance.Options);
		init.Arguments[3].Should().BeOfType<DotNetObjectReference<PDZoomBar>>();
	}

	/// <summary>A value reported from JavaScript replaces the current value and is raised.</summary>
	[Fact]
	public async Task OnValueChanged_FromJavaScript_ReplacesValueAndRaises()
	{
		var cut = RenderZoomBar(100);
		var reported = new ZoombarValue { Zoom = 30, Pan = 12.5 };

		await cut.InvokeAsync(() => cut.Instance.OnValueChanged(reported));

		cut.Instance.Value.Should().BeSameAs(reported);
		_changes.Should().Equal(30);
	}

	/// <summary>When the module cannot be imported (strict interop with nothing planned throws) the buttons still step the value without any module call.</summary>
	[Fact]
	public void ImportFailure_IsSwallowed_AndZoomStillWorks()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var cut = RenderZoomBar(100);

		cut.FindAll("button")[1].Click();
		cut.FindAll("button")[0].Click();

		_changes.Should().Equal(90, 100);
	}

	/// <summary>Disposing the component tells the module to release the canvas.</summary>
	[Fact]
	public async Task Dispose_TellsTheModuleToReleaseTheCanvas()
	{
		var module = JSInterop.SetupModule(ModulePath);
		RenderZoomBar(100);

		await DisposeComponentsAsync();

		module.Invocations["dispose"].Should().ContainSingle()
			.Which.Arguments[0].Should().Be("zb-canvas");
	}

	/// <summary>A failure while disposing the module is swallowed rather than surfacing from disposal.</summary>
	[Fact]
	public async Task Dispose_WhenTheModuleThrows_DoesNotThrow()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("dispose", _ => true).SetException(new JSException("gone"));
		var cut = RenderZoomBar(100);

		var act = async () => await cut.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
	}

	/// <summary>Disposing a component whose module never loaded does nothing.</summary>
	[Fact]
	public async Task Dispose_WithoutAModule_DoesNotThrow()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var cut = RenderZoomBar(100);

		var act = async () => await cut.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
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
