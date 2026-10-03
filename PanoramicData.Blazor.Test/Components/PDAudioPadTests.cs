using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDAudioPad"/> draws itself from its value and toggles or decays when pressed.
/// </summary>
public partial class PDAudioPadTests : BunitContext
{
	private readonly List<PDAudioPadEventArgs> _events = [];
	private readonly List<double> _values = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDAudioPadTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<PDAudioPad> RenderPad(Action<ComponentParameterCollectionBuilder<PDAudioPad>>? configure = null)
		=> Render<PDAudioPad>(parameters =>
		{
			parameters
				.Add(p => p.Label, "Kick")
				.Add(p => p.OnPadValueChanged, (PDAudioPadEventArgs e) => _events.Add(e))
				.Add(p => p.ValueChanged, (double v) => _values.Add(v));
			configure?.Invoke(parameters);
		});

	/// <summary>The pad is drawn at its size, coloured between the inactive and active colours.</summary>
	[Theory]
	[InlineData(0.0, "#444444")]
	[InlineData(1.0, "#FFFF00")]
	public void Colour_FollowsTheValue(double value, string expectedFill)
	{
		var component = RenderPad(p => p
			.Add(x => x.Value, value)
			.Add(x => x.Width, 80)
			.Add(x => x.Height, 40));

		var svg = component.Find("svg");
		svg.GetAttribute("width").Should().Be("80");
		svg.GetAttribute("height").Should().Be("40");
		component.Find("rect").GetAttribute("fill").Should().Be(expectedFill);
	}

	/// <summary>The glow filter is applied only while the pad is more than half on.</summary>
	[Theory]
	[InlineData(0.9, true)]
	[InlineData(0.5, false)]
	public void Glow_IsAppliedAboveHalf(double value, bool expectGlow)
	{
		var component = RenderPad(p => p.Add(x => x.Value, value));

		component.Find("rect").HasAttribute("filter").Should().Be(expectGlow);
	}

	/// <summary>A symbol maps to its icon, coloured for contrast unless a colour is given.</summary>
	[Theory]
	[InlineData(Symbol.Play, "fa-play")]
	[InlineData(Symbol.Pause, "fa-pause")]
	[InlineData(Symbol.PreviousTrack, "fa-backward")]
	[InlineData(Symbol.NextTrack, "fa-forward")]
	public void Symbol_MapsToItsIcon(Symbol symbol, string iconClass)
	{
		var low = RenderPad(p => p.Add(x => x.Symbol, symbol).Add(x => x.Value, 0.0));
		var high = RenderPad(p => p.Add(x => x.Symbol, symbol).Add(x => x.Value, 1.0));

		low.Find(".pd-audio-pad-symbol").ClassList.Should().Contain(iconClass);
		low.Find(".pd-audio-pad-symbol").GetAttribute("style").Should().Contain("white");
		high.Find(".pd-audio-pad-symbol").GetAttribute("style").Should().Contain("black");
	}

	/// <summary>An explicit symbol colour overrides the contrast colour; Symbol.None shows no icon.</summary>
	[Fact]
	public void SymbolColourAndNone_AreHonoured()
	{
		var coloured = RenderPad(p => p.Add(x => x.Symbol, Symbol.Play).Add(x => x.SymbolColor, "red"));
		var none = RenderPad(p => p.Add(x => x.Symbol, Symbol.None));
		var absent = RenderPad();

		coloured.Find(".pd-audio-pad-symbol").GetAttribute("style").Should().Contain("red");
		none.Find(".pd-audio-pad-symbol").ClassList.Should().ContainSingle();
		absent.FindAll(".pd-audio-pad-symbol").Should().BeEmpty();
	}

	/// <summary>The label is drawn above, below or over the pad as positioned.</summary>
	[Theory]
	[InlineData(PDLabelPosition.Above)]
	[InlineData(PDLabelPosition.Below)]
	public void Label_IsDrawnAboveOrBelow(PDLabelPosition position)
	{
		var component = RenderPad(p => p.Add(x => x.LabelPosition, position).Add(x => x.LabelCssClass, "lbl"));

		var children = component.Find("div.pd-audio-pad").Children;
		var labelIndex = position == PDLabelPosition.Above ? 0 : 1;
		children[labelIndex].ClassList.Should().Contain(["pd-audio-label", "lbl"]);
		children[labelIndex].TextContent.Should().Be("Kick");
		children[1 - labelIndex].TagName.Should().BeEquivalentTo("svg");
	}

	/// <summary>An overlay label is drawn inside the pad, in the given colour or black.</summary>
	[Fact]
	public void OverlayLabel_IsDrawnInsideThePad()
	{
		var defaultColour = RenderPad(p => p.Add(x => x.LabelPosition, PDLabelPosition.Overlay));
		var red = RenderPad(p => p.Add(x => x.LabelPosition, PDLabelPosition.Overlay).Add(x => x.LabelColor, "red"));

		defaultColour.FindAll(".pd-audio-label").Should().BeEmpty();
		var overlay = defaultColour.Find("svg foreignObject div");
		overlay.TextContent.Trim().Should().Be("Kick");
		overlay.GetAttribute("style").Should().Contain("color: black");
		red.Find("svg foreignObject div").GetAttribute("style").Should().Contain("color: red");
	}

	/// <summary>In toggle mode each press flips the pad on and off, raising both events every time.</summary>
	[Fact]
	public async Task Toggle_FlipsOnEachPress()
	{
		var component = RenderPad(p => p.Add(x => x.Value, 0.0).Add(x => x.MinValue, 0.1));

		await component.Find("svg").MouseDownAsync(new MouseEventArgs());
		await component.Find("svg").MouseDownAsync(new MouseEventArgs());

		_values.Should().Equal(1.0, 0.1);
		_events.Select(e => e.IsActive).Should().Equal(true, false);
		_events.Should().AllSatisfy(e =>
		{
			e.Label.Should().Be("Kick");
			e.DecayMode.Should().Be(DecayMode.Toggle);
		});
		component.Instance.Value.Should().Be(0.1);
	}
}
