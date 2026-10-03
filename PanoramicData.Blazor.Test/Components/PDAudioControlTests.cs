using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for the <see cref="PDAudioControl"/> base class: snapping, drag handling through the JavaScript module,
/// double-click reset, the marking step calculation and the label fragment. A minimal derived control is used so
/// the base behaviour is tested on its own.
/// </summary>
public partial class PDAudioControlTests : BunitContext
{
	private const string AudioModulePath = "./test-audio.js";

	private readonly List<double> _values = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDAudioControlTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<TestAudioControl> RenderControl(Action<ComponentParameterCollectionBuilder<TestAudioControl>>? configure = null)
		=> Render<TestAudioControl>(p =>
		{
			p.Add(x => x.ValueChanged, (double v) => _values.Add(v));
			configure?.Invoke(p);
		});

	/// <summary>A missing default value is filled in as the midpoint.</summary>
	[Fact]
	public void DefaultValue_DefaultsToHalf()
	{
		var cut = RenderControl();

		cut.Instance.DefaultValue.Should().Be(0.5);
		cut.Instance.SnapIncrement.Should().Be(0);
	}

	/// <summary>Snap points set the snap increment to one over the number of intervals.</summary>
	[Fact]
	public void SnapPoints_SetTheSnapIncrement()
	{
		var cut = RenderControl(p => p.Add(x => x.SnapPoints, 5));

		cut.Instance.SnapIncrement.Should().Be(0.25);
		_values.Should().BeEmpty();
	}

	/// <summary>Changing the snap points snaps the current value to the new grid and reports it.</summary>
	[Fact]
	public void ChangingSnapPoints_SnapsTheValue()
	{
		var cut = RenderControl(p => p.Add(x => x.SnapPoints, 5).Add(x => x.Value, 0.3));

		cut.Render(p => p.Add(x => x.SnapPoints, 3));

		cut.Instance.SnapIncrement.Should().Be(0.5);
		_values.Should().Equal(0.5);
	}

	/// <summary>Changing the snap points when the value is already on the new grid reports nothing.</summary>
	[Fact]
	public void ChangingSnapPoints_WhenAlreadyOnGrid_ReportsNothing()
	{
		var cut = RenderControl(p => p.Add(x => x.SnapPoints, 5).Add(x => x.Value, 0.5));

		cut.Render(p => p.Add(x => x.SnapPoints, 3));

		_values.Should().BeEmpty();
	}

	/// <summary>Reducing snap points to one turns snapping off and reports nothing.</summary>
	[Fact]
	public void ChangingSnapPointsToOne_TurnsSnappingOff()
	{
		var cut = RenderControl(p => p.Add(x => x.SnapPoints, 5).Add(x => x.Value, 0.3));

		cut.Render(p => p.Add(x => x.SnapPoints, 1));

		cut.Instance.SnapIncrement.Should().Be(0);
		_values.Should().BeEmpty();
	}

	/// <summary>Double-click resets to the default value.</summary>
	[Fact]
	public void DoubleClick_ResetsToDefault()
	{
		var cut = RenderControl(p => p.Add(x => x.DefaultValue, 0.8).Add(x => x.Value, 0.1));

		cut.Find("div.audio").DoubleClick();

		_values.Should().Equal(0.8);
	}

	/// <summary>Double-click with snapping resets to the default value snapped to the grid.</summary>
	[Fact]
	public void DoubleClick_WithSnapping_SnapsTheDefault()
	{
		var cut = RenderControl(p => p.Add(x => x.DefaultValue, 0.8).Add(x => x.SnapPoints, 3));

		cut.Find("div.audio").DoubleClick();

		_values.Should().Equal(1);
	}

	/// <summary>The marking step picks a readable interval for the range.</summary>
	/// <param name="max">The maximum of the range.</param>
	/// <param name="expected">The expected step.</param>
	[Theory]
	[InlineData(5, 1)]
	[InlineData(12, 1)]
	[InlineData(15, 2)]
	[InlineData(40, 5)]
	[InlineData(80, 10)]
	[InlineData(150, 20)]
	[InlineData(400, 50)]
	[InlineData(900, 100)]
	[InlineData(2500, 250)]
	public void CalculateMarkingStep_PicksAReadableInterval(int max, int expected)
		=> TestAudioControl.MarkingStep(max).Should().Be(expected);

	/// <summary>A label is rendered with the configured class and height.</summary>
	[Fact]
	public void Label_IsRenderedWithClassAndHeight()
	{
		var cut = RenderControl(p => p.Add(x => x.Label, "Gain").Add(x => x.LabelCssClass, "lbl").Add(x => x.LabelHeightPx, 30));

		var label = cut.Find("div.pd-audio-label");
		label.ClassList.Should().Contain("lbl");
		label.TextContent.Should().Be("Gain");
		label.GetAttribute("style").Should().Contain("height:30px");
	}

	/// <summary>Without a label no label element is rendered.</summary>
	[Fact]
	public void NoLabel_RendersNoLabel()
	{
		var cut = RenderControl();

		cut.FindAll("div.pd-audio-label").Should().BeEmpty();
	}

	/// <summary>A minimal control that exposes the base class behaviour.</summary>
	private sealed class TestAudioControl : PDAudioControl
	{
		/// <summary>Gets or sets the module file this control imports.</summary>
		[Parameter] public string? ModulePath { get; set; }

		protected override string JsFileName => ModulePath ?? base.JsFileName;

		public static int MarkingStep(int max) => CalculateMarkingStep(max);

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "class", "audio");
			builder.AddAttribute(2, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerDown));
			builder.AddAttribute(3, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnDoubleClick));
			builder.AddContent(4, RenderLabel());
			builder.CloseElement();
		}
	}
}
