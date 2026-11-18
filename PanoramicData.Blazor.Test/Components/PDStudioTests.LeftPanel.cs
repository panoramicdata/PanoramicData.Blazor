using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;
using System.Globalization;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The left panel's width, which a host may give and may let the user change from the panel's edge.
/// </summary>
public partial class PDStudioTests
{
	private const string StudioModule = "./_content/PanoramicData.Blazor/PDStudio.razor.js";

	private IRenderedComponent<PDStudio> RenderWithLeftPanel(Action<ComponentParameterCollectionBuilder<PDStudio>> more)
		=> RenderWithLeftPanel(new PDStudioOptions(), more);

	private IRenderedComponent<PDStudio> RenderWithLeftPanel(PDStudioOptions options, Action<ComponentParameterCollectionBuilder<PDStudio>> more)
		=> RenderStudio(options, p =>
		{
			p.Add(x => x.LeftPanelContent, b => b.AddMarkupContent(0, "<div class=\"schema\">Schema</div>"));
			more(p);
		});

	private static string? LeftPanelStyle(IRenderedComponent<PDStudio> studio)
		=> studio.Find(".pd-studio-left-panel").GetAttribute("style");

	/// <summary>
	/// Verifies that by default the left panel has no edge to drag and no width of its own, so it is as it always was.
	/// </summary>
	[Fact]
	public void LeftPanel_ByDefault_HasNoEdgeToDrag_AndNoWidthOfItsOwn()
	{
		var studio = RenderWithLeftPanel(_ => { });

		LeftPanelStyle(studio).Should().BeNull();
		studio.Find(".pd-studio-left-panel").ClassList.Should().NotContain("pd-studio-left-panel-resizable");
		studio.FindAll(".pd-studio-left-panel-handle").Should().BeEmpty();
		JSInterop.Invocations["import"].Select(i => i.Arguments[0]).Should().NotContain(StudioModule);
	}

	/// <summary>
	/// Verifies that a resizable left panel has a bar on its edge that can take focus and gives its limits and the
	/// options' tooltip, and has the class gutter, so a host's gutter styling reaches it.
	/// </summary>
	[Fact]
	public void LeftPanelResizable_PutsABarOnItsEdge_ThatGivesItsLimits()
	{
		var options = new PDStudioOptions { LeftPanelResizeToolTip = "Drag me" };
		var studio = RenderWithLeftPanel(options, p => p
			.Add(x => x.LeftPanelResizable, true)
			.Add(x => x.LeftPanelMinWidth, 12)
			.Add(x => x.LeftPanelMaxWidth, 30.5));

		var handle = studio.Find(".pd-studio-left-panel-handle");
		handle.ClassList.Should().Contain("gutter");
		handle.GetAttribute("role").Should().Be("separator");
		handle.GetAttribute("tabindex").Should().Be("0");
		handle.GetAttribute("aria-orientation").Should().Be("vertical");
		handle.GetAttribute("aria-valuemin").Should().Be("12");
		handle.GetAttribute("aria-valuemax").Should().Be("30.5");
		handle.HasAttribute("aria-valuenow").Should().BeFalse("the stylesheet's width is not known until the user resizes the panel");
		handle.GetAttribute("title").Should().Be("Drag me");
		handle.GetAttribute("aria-label").Should().Be("Drag me");
		studio.Find(".pd-studio-left-panel").ClassList.Should().Contain("pd-studio-left-panel-resizable");
	}

	/// <summary>Verifies that with no left panel there is no edge to drag, whatever LeftPanelResizable says.</summary>
	[Fact]
	public void LeftPanelResizable_WithoutALeftPanel_PutsNoBarAnywhere()
		=> RenderStudio(more: p => p.Add(x => x.LeftPanelResizable, true)).FindAll(".pd-studio-left-panel-handle").Should().BeEmpty();

	/// <summary>
	/// Verifies that a width the host gives is set on the panel in rem with a full stop, even in a culture that writes
	/// a decimal comma, and whether or not the user may resize it.
	/// </summary>
	[Fact]
	public void LeftPanelWidth_IsSetOnThePanel_InRem_WhateverTheCulture()
	{
		var culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			var studio = RenderWithLeftPanel(p => p.Add(x => x.LeftPanelWidth, 18.5));

			LeftPanelStyle(studio).Should().Be("--pd-studio-left-panel-width: 18.5rem");
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	/// <summary>Verifies that a width the host gives outside the limits is kept within them.</summary>
	[Theory]
	[InlineData(4, "10")]
	[InlineData(55, "40")]
	public void LeftPanelWidth_OutsideTheLimits_IsKeptWithinThem(double given, string expected)
		=> LeftPanelStyle(RenderWithLeftPanel(p => p.Add(x => x.LeftPanelWidth, given)))
			.Should().Be($"--pd-studio-left-panel-width: {expected}rem");

	/// <summary>
	/// Verifies that resizing the edge, as its script reports, sets the panel's width within the limits and raises
	/// LeftPanelWidthChanged with it.
	/// </summary>
	[Fact]
	public async Task ResizingTheEdge_SetsTheWidthWithinTheLimits_AndRaisesLeftPanelWidthChanged()
	{
		var raised = new List<double?>();
		var studio = RenderWithLeftPanel(p => p
			.Add(x => x.LeftPanelResizable, true)
			.Add(x => x.LeftPanelWidthChanged, (double? width) => raised.Add(width)));

		await studio.InvokeAsync(() => studio.Instance.OnLeftPanelResized(22.25));

		LeftPanelStyle(studio).Should().Be("--pd-studio-left-panel-width: 22.25rem");
		studio.Find(".pd-studio-left-panel-handle").GetAttribute("aria-valuenow").Should().Be("22.25");

		await studio.InvokeAsync(() => studio.Instance.OnLeftPanelResized(80));

		LeftPanelStyle(studio).Should().Be("--pd-studio-left-panel-width: 40rem");
		raised.Should().Equal(22.25, 40);
	}

	/// <summary>
	/// Verifies that double-clicking the edge gives the panel back the stylesheet's width and raises
	/// LeftPanelWidthChanged with null.
	/// </summary>
	[Fact]
	public async Task DoubleClickingTheEdge_GivesThePanelBackItsUsualWidth()
	{
		var raised = new List<double?>();
		var studio = RenderWithLeftPanel(p => p
			.Add(x => x.LeftPanelResizable, true)
			.Add(x => x.LeftPanelWidth, 25)
			.Add(x => x.LeftPanelWidthChanged, (double? width) => raised.Add(width)));

		await studio.Find(".pd-studio-left-panel-handle").DoubleClickAsync(new MouseEventArgs());

		LeftPanelStyle(studio).Should().BeNull();
		studio.Find(".pd-studio-left-panel-handle").HasAttribute("aria-valuenow").Should().BeFalse();
		raised.Should().Equal((double?)null);
	}

	/// <summary>
	/// Verifies that a width the user resized to stays while the host keeps giving the same width, and that a different
	/// width from the host, or none, replaces it.
	/// </summary>
	[Fact]
	public async Task AWidthTheUserResizedTo_StaysUntilTheHostGivesAnother()
	{
		var studio = RenderWithLeftPanel(p => p
			.Add(x => x.LeftPanelResizable, true)
			.Add(x => x.LeftPanelWidth, 20));
		await studio.InvokeAsync(() => studio.Instance.OnLeftPanelResized(26));

		studio.Render(p => p.Add(x => x.LeftPanelWidth, 20));
		LeftPanelStyle(studio).Should().Be("--pd-studio-left-panel-width: 26rem");

		studio.Render(p => p.Add(x => x.LeftPanelWidth, 30));
		LeftPanelStyle(studio).Should().Be("--pd-studio-left-panel-width: 30rem");

		studio.Render(p => p.Add(x => x.LeftPanelWidth, (double?)null));
		LeftPanelStyle(studio).Should().BeNull();
	}

	/// <summary>
	/// Verifies that a host's width handler failing is logged rather than thrown, from the script's resize and from a
	/// double-click, so the circuit carries on and the panel still takes the width.
	/// </summary>
	[Fact]
	public async Task AHostsWidthHandlerFailing_IsNotThrown_AndThePanelStillTakesTheWidth()
	{
		Action<double?> fail = _ => throw new InvalidOperationException("The host failed");
		var studio = RenderWithLeftPanel(p => p
			.Add(x => x.LeftPanelResizable, true)
			.Add(x => x.LeftPanelWidth, 25)
			.Add(x => x.LeftPanelWidthChanged, fail));

		await studio.InvokeAsync(() => studio.Instance.OnLeftPanelResized(30));
		LeftPanelStyle(studio).Should().Be("--pd-studio-left-panel-width: 30rem");

		await studio.Find(".pd-studio-left-panel-handle").DoubleClickAsync(new MouseEventArgs());
		LeftPanelStyle(studio).Should().BeNull();
	}

	/// <summary>Verifies that a width that is not a real number, from the host or the script, is ignored.</summary>
	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	public async Task AWidthThatIsNotARealNumber_IsIgnored(double width)
	{
		var raised = new List<double?>();
		var studio = RenderWithLeftPanel(p => p
			.Add(x => x.LeftPanelResizable, true)
			.Add(x => x.LeftPanelWidth, width)
			.Add(x => x.LeftPanelWidthChanged, (double? changed) => raised.Add(changed)));

		LeftPanelStyle(studio).Should().BeNull();

		await studio.InvokeAsync(() => studio.Instance.OnLeftPanelResized(width));

		LeftPanelStyle(studio).Should().BeNull();
		raised.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the edge's script failing as it starts, in the script or in reaching it, is logged rather than
	/// thrown after the render, where it would end the circuit, and that the studio carries on with the edge in place.
	/// </summary>
	[Theory]
	[InlineData("fails")]
	[InlineData("cannot be reached")]
	public void TheEdgesScriptFailingToStart_IsNotThrown_AndTheStudioCarriesOn(string how)
	{
		Exception failure = how == "fails" ? new JSException("The script failed") : new InvalidOperationException("The script cannot be reached");
		var module = JSInterop.SetupModule(StudioModule);
		module.SetupVoid("initializeLeftPanelHandle", _ => true).SetException(failure);

		var studio = RenderWithLeftPanel(p => p.Add(x => x.LeftPanelResizable, true));
		studio.WaitForAssertion(() => module.VerifyInvoke("initializeLeftPanelHandle", 1));
		studio.Render();

		Renderer.UnhandledException.IsCompleted.Should().BeFalse("a failure thrown after a render ends the circuit");
		studio.FindAll(".pd-studio-left-panel-handle").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that the edge is handed to its script once while it is shown, and again when it is shown afresh after the
	/// host stopped the panel being resizable.
	/// </summary>
	[Fact]
	public void TheEdge_IsHandedToItsScript_OnceEachTimeItIsShown()
	{
		var module = JSInterop.SetupModule(StudioModule);
		var studio = RenderWithLeftPanel(p => p.Add(x => x.LeftPanelResizable, true));
		studio.WaitForAssertion(() => module.VerifyInvoke("initializeLeftPanelHandle", 1));

		studio.Render(p => p.Add(x => x.LeftPanelWidth, 22));
		module.VerifyInvoke("initializeLeftPanelHandle", 1);

		studio.Render(p => p.Add(x => x.LeftPanelResizable, false));
		studio.Render(p => p.Add(x => x.LeftPanelResizable, true));
		studio.WaitForAssertion(() => module.VerifyInvoke("initializeLeftPanelHandle", 2));
	}
}
