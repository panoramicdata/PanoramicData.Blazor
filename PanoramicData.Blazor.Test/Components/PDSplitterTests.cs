using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDSplitter"/>: the panels it lays out and what it asks split.js to do with them.
/// </summary>
public class PDSplitterTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDSplitter.razor.js";

	private IRenderedComponent<PDSplitter> RenderSplitter(SplitDirection direction = SplitDirection.Horizontal)
		=> Render<PDSplitter>(parameters => parameters
			.Add(p => p.Direction, direction)
			.Add(p => p.CssClass, "my-splitter")
			.Add(p => p.GutterSize, 6)
			.Add(p => p.SnapOffset, 12)
			.Add(p => p.DragInterval, 2)
			.Add(p => p.ExpandToMin, true)
			.AddChildContent<PDSplitPanel>(panel => panel
				.Add(p => p.Size, 1)
				.Add(p => p.MinSize, 50)
				.Add(p => p.CssClass, "left")
				.AddChildContent("<span class=\"left-content\">Left</span>"))
			.AddChildContent<PDSplitPanel>(panel => panel
				.Add(p => p.Size, 3)
				.Add(p => p.CssClass, "right")
				.AddChildContent("<span class=\"right-content\">Right</span>")));

	private BunitJSModuleInterop SetupModule(bool hasSplitJs = true)
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.Setup<bool>("hasSplitJs").SetResult(hasSplitJs);
		module.SetupVoid("initialize", _ => true).SetVoidResult();
		return module;
	}

	/// <summary>
	/// Verifies that each child panel is rendered as a split panel carrying its content, id and CSS class.
	/// </summary>
	[Theory]
	[InlineData(SplitDirection.Horizontal, "horizontal")]
	[InlineData(SplitDirection.Vertical, "vertical")]
	public void Panels_AreRenderedInADirectionalContainer(SplitDirection direction, string directionClass)
	{
		SetupModule();

		var splitter = RenderSplitter(direction);

		var container = splitter.Find("div.pdsplitter");
		container.ClassList.Should().Contain(directionClass).And.Contain("my-splitter");
		var panels = splitter.FindAll("div.pdsplitpanel");
		panels.Should().HaveCount(2);
		panels[0].ClassList.Should().Contain("left");
		panels[0].Id.Should().NotBeNullOrEmpty();
		panels[0].TextContent.Should().Contain("Left");
		panels[1].ClassList.Should().Contain("right");
		panels[1].TextContent.Should().Contain("Right");
	}

	/// <summary>
	/// Verifies that split.js is initialised with the panel ids, their sizes as percentages and the configured options.
	/// </summary>
	[Fact]
	public void FirstRender_InitialisesSplitJs_WithThePanelsAndOptions()
	{
		var module = SetupModule();

		var splitter = RenderSplitter();

		var initialize = module.VerifyInvoke("initialize");
		initialize.Arguments[0].Should().Be(splitter.Instance.Id);
		var panelIds = splitter.FindAll("div.pdsplitpanel").Select(p => $"#{p.Id}");
		initialize.Arguments[1].Should().BeOfType<string[]>().Which.Should().Equal(panelIds);
		var options = initialize.Arguments[2].Should().BeOfType<SplitOptions>().Subject;
		options.Sizes.Should().Equal(25, 75);
		options.MinSize.Should().Equal(50, 100);
		options.Direction.Should().Be("horizontal");
		options.Cursor.Should().Be("col-resize");
		options.GutterSize.Should().Be(6);
		options.SnapOffset.Should().Be(12);
		options.DragInterval.Should().Be(2);
		options.ExpandToMin.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the gutter alignment is passed to split.js (#178), and defaults to "center".
	/// </summary>
	[Theory]
	[InlineData("start")]
	[InlineData("end")]
	public void GutterAlign_IsPassedToSplitJs(string align)
	{
		var module = SetupModule();

		Render<PDSplitter>(parameters => parameters
			.Add(p => p.GutterAlign, align)
			.AddChildContent<PDSplitPanel>(panel => panel.Add(p => p.Size, 1))
			.AddChildContent<PDSplitPanel>(panel => panel.Add(p => p.Size, 1)));

		var options = module.VerifyInvoke("initialize").Arguments[2].Should().BeOfType<SplitOptions>().Subject;
		options.GutterAlign.Should().Be(align);
	}

	/// <summary>
	/// Verifies that without a GutterAlign split.js is asked to centre the gutter, as before.
	/// </summary>
	[Fact]
	public void GutterAlign_DefaultsToCenter()
	{
		var module = SetupModule();

		RenderSplitter();

		var options = module.VerifyInvoke("initialize").Arguments[2].Should().BeOfType<SplitOptions>().Subject;
		options.GutterAlign.Should().Be("center");
	}

	/// <summary>
	/// Verifies that a vertical splitter asks for a row resize cursor.
	/// </summary>
	[Fact]
	public void VerticalSplitter_UsesARowResizeCursor()
	{
		var module = SetupModule();

		RenderSplitter(SplitDirection.Vertical);

		var options = module.VerifyInvoke("initialize").Arguments[2].Should().BeOfType<SplitOptions>().Subject;
		options.Direction.Should().Be("vertical");
		options.Cursor.Should().Be("row-resize");
	}

	/// <summary>
	/// Verifies that without the split.js library nothing is initialised and the page does not fail.
	/// </summary>
	[Fact]
	public void WithoutSplitJs_NothingIsInitialised()
	{
		var module = SetupModule(hasSplitJs: false);

		var splitter = RenderSplitter();

		module.Invocations["initialize"].Should().BeEmpty();
		splitter.FindAll("div.pdsplitpanel").Should().HaveCount(2);
	}

	/// <summary>
	/// Verifies that the current sizes are read from split.js for this splitter.
	/// </summary>
	[Fact]
	public async Task GetSizes_ReturnsTheSizesReportedBySplitJs()
	{
		var module = SetupModule();
		module.Setup<double[]>("getSizes", _ => true).SetResult([30.5, 69.5]);
		var splitter = RenderSplitter();

		var sizes = await splitter.InvokeAsync(() => splitter.Instance.GetSizesAsync());

		sizes.Should().Equal(30.5, 69.5);
		module.VerifyInvoke("getSizes").Arguments[0].Should().Be(splitter.Instance.Id);
	}

	/// <summary>
	/// Verifies that new sizes are passed to split.js for this splitter.
	/// </summary>
	[Fact]
	public async Task SetSizes_PassesTheSizesToSplitJs()
	{
		var module = SetupModule();
		module.SetupVoid("setSizes", _ => true).SetVoidResult();
		var splitter = RenderSplitter();

		await splitter.InvokeAsync(() => splitter.Instance.SetSizesAsync([40, 60]));

		var invocation = module.VerifyInvoke("setSizes");
		invocation.Arguments[0].Should().Be(splitter.Instance.Id);
		invocation.Arguments[1].Should().BeOfType<double[]>().Which.Should().Equal(40, 60);
	}

	/// <summary>
	/// Verifies that when the module could not be loaded, reading sizes returns none and setting them does nothing.
	/// </summary>
	[Fact]
	public async Task WithoutTheModule_SizesAreEmptyAndSettingThemIsHarmless()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var splitter = RenderSplitter();

		var sizes = await splitter.InvokeAsync(() => splitter.Instance.GetSizesAsync());
		var set = async () => await splitter.InvokeAsync(() => splitter.Instance.SetSizesAsync([50, 50]));

		sizes.Should().BeEmpty();
		await set.Should().NotThrowAsync();
	}

	/// <summary>
	/// Verifies that disposing the splitter tears down its split.js instance.
	/// </summary>
	[Fact]
	public async Task Dispose_DestroysTheSplitJsInstance()
	{
		var module = SetupModule();
		module.SetupVoid("destroy", _ => true).SetVoidResult();
		var splitter = RenderSplitter();
		var id = splitter.Instance.Id;

		await DisposeComponentsAsync();

		module.VerifyInvoke("destroy").Arguments[0].Should().Be(id);
	}

	/// <summary>
	/// Verifies that each splitter instance gets its own id.
	/// </summary>
	[Fact]
	public void EachSplitter_HasADistinctId()
	{
		SetupModule();

		var first = RenderSplitter();
		var second = RenderSplitter();

		first.Instance.Id.Should().StartWith("pdsplit-").And.NotBe(second.Instance.Id);
	}
}
