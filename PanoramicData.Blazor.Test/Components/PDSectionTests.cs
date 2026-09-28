using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDSection"/> renders an accessible collapsible header and body, and toggles on click
/// and through its public methods.
/// </summary>
public class PDSectionTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDSectionTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies the default, expanded markup: generated ids tie the header to the body, the chevron points down,
	/// and the title is a plain span.
	/// </summary>
	[Fact]
	public void Default_RendersExpandedAccessibleMarkup()
	{
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.Title, "Settings")
			.Add(p => p.CssClass, "outer")
			.Add(p => p.HeaderCssClass, "hdr")
			.Add(p => p.BodyCssClass, "inner")
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p class=\"content\">Hello</p>"))));

		var id = component.Instance.Id;
		id.Should().StartWith("pd-section-");

		var root = component.Find("div.pd-section");
		root.Id.Should().Be(id);
		root.ClassList.Should().Contain("outer");

		var header = component.Find("button.pd-section-header");
		header.Id.Should().Be($"{id}-header");
		header.ClassList.Should().Contain("hdr");
		header.ClassList.Should().NotContain("pd-section-header--disabled");
		header.GetAttribute("aria-controls").Should().Be($"{id}-body");
		header.GetAttribute("title").Should().Be("Click to expand or collapse");
		header.HasAttribute("disabled").Should().BeFalse();

		component.Find(".pd-section-chevron i").ClassList.Should().Contain("fa-chevron-down");
		component.Find("span.pd-section-title").TextContent.Should().Be("Settings");
		component.FindAll(".pd-section-secondary-title").Should().BeEmpty();
		component.FindAll(".pd-section-header-actions").Should().BeEmpty();

		var body = component.Find("div.pd-section-body");
		body.Id.Should().Be($"{id}-body");
		body.GetAttribute("aria-labelledby").Should().Be($"{id}-header");
		body.ClassList.Should().NotContain("pd-section-body--collapsed");
		component.Find(".pd-section-body-inner.inner p.content").TextContent.Should().Be("Hello");
	}

	/// <summary>
	/// Verifies that two sections are given different ids.
	/// </summary>
	[Fact]
	public void Ids_AreUniquePerInstance()
	{
		var first = Render<PDSection>();
		var second = Render<PDSection>();

		first.Instance.Id.Should().NotBe(second.Instance.Id);
	}

	/// <summary>
	/// Verifies that a collapsed section shows a right chevron, and a collapsed body.
	/// </summary>
	[Fact]
	public void Collapsed_RendersCollapsedState()
	{
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.IsCollapsed, true)
			.Add(p => p.ExpanderTooltip, "Toggle me"));

		component.Find("button").GetAttribute("title").Should().Be("Toggle me");
		component.Find(".pd-section-chevron i").ClassList.Should().Contain("fa-chevron-right");
		component.Find(".pd-section-body").ClassList.Should().Contain("pd-section-body--collapsed");
	}

	/// <summary>
	/// Verifies that each heading level renders the matching H element, with the secondary title inside it.
	/// </summary>
	/// <param name="level">The heading level.</param>
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	public void HeadingLevel_RendersMatchingHeading(int level)
	{
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.Title, "Main")
			.Add(p => p.SecondaryTitle, "(3)")
			.Add(p => p.TitleCssClass, "t")
			.Add(p => p.SecondaryTitleCssClass, "sec")
			.Add(p => p.HeadingLevel, level));

		var heading = component.Find($"h{level}.pd-section-title.t");
		heading.TextContent.Should().Be("Main(3)");
		heading.QuerySelector("span.pd-section-secondary-title.sec")!.TextContent.Should().Be("(3)");
	}

	/// <summary>
	/// Verifies that a heading level outside 1 to 6 falls back to the plain span layout, with the secondary title
	/// beside the title.
	/// </summary>
	[Fact]
	public void OutOfRangeHeadingLevel_FallsBackToSpans()
	{
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.Title, "Main")
			.Add(p => p.SecondaryTitle, "extra")
			.Add(p => p.HeadingLevel, 7));

		component.FindAll("h1, h2, h3, h4, h5, h6").Should().BeEmpty();
		component.Find("span.pd-section-title").TextContent.Should().Be("Main");
		var secondary = component.Find("span.pd-section-secondary-title");
		secondary.TextContent.Should().Be("extra");
		secondary.ClassList.Should().Contain(["text-muted", "ms-2", "fw-normal"]);
	}

	/// <summary>
	/// Verifies that a heading with no secondary title renders just the title.
	/// </summary>
	[Fact]
	public void HeadingWithoutSecondaryTitle_RendersTitleOnly()
	{
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.Title, "Main")
			.Add(p => p.HeadingLevel, 2));

		component.Find("h2").TextContent.Should().Be("Main");
		component.FindAll(".pd-section-secondary-title").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a title template replaces the title, secondary title and heading.
	/// </summary>
	[Fact]
	public void TitleTemplate_ReplacesTitle()
	{
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.Title, "Ignored")
			.Add(p => p.HeadingLevel, 1)
			.Add(p => p.TitleTemplate, (RenderFragment)(b => b.AddMarkupContent(0, "<b class=\"custom\">Custom</b>"))));

		component.Find(".pd-section-title-area b.custom").TextContent.Should().Be("Custom");
		component.FindAll(".pd-section-title").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that header actions are rendered inside the header, in their own container.
	/// </summary>
	[Fact]
	public void HeaderActions_AreRenderedInTheHeader()
	{
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.HeaderActions, (RenderFragment)(b => b.AddMarkupContent(0, "<a class=\"act\">Edit</a>"))));

		component.Find("button.pd-section-header .pd-section-header-actions a.act").TextContent.Should().Be("Edit");
	}

	/// <summary>
	/// Verifies that clicking the header collapses then expands the section, raising the binding callback and
	/// the toggled callback with the new state each time.
	/// </summary>
	[Fact]
	public void ClickingHeader_TogglesAndRaisesCallbacks()
	{
		var changed = new List<bool>();
		var toggled = new List<bool>();
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.IsCollapsedChanged, (bool v) => changed.Add(v))
			.Add(p => p.Toggled, (bool v) => toggled.Add(v)));

		component.Find("button.pd-section-header").Click();
		component.Find(".pd-section-body").ClassList.Should().Contain("pd-section-body--collapsed");
		component.Find(".pd-section-chevron i").ClassList.Should().Contain("fa-chevron-right");

		component.Find("button.pd-section-header").Click();
		component.Find(".pd-section-body").ClassList.Should().NotContain("pd-section-body--collapsed");

		changed.Should().Equal(true, false);
		toggled.Should().Equal(true, false);
	}

	/// <summary>
	/// Verifies that a disabled section renders a disabled header with the disabled class.
	/// </summary>
	[Fact]
	public void Disabled_RendersDisabledHeader()
	{
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.IsDisabled, true));

		var header = component.Find("button.pd-section-header");
		header.HasAttribute("disabled").Should().BeTrue();
		header.ClassList.Should().Contain("pd-section-header--disabled");
	}

	/// <summary>
	/// Verifies that unmatched attributes are applied to the outer container.
	/// </summary>
	[Fact]
	public void AdditionalAttributes_AreAppliedToContainer()
	{
		var component = Render<PDSection>(parameters => parameters
			.AddUnmatched("data-test", "section-1"));

		component.Find("div.pd-section").GetAttribute("data-test").Should().Be("section-1");
	}

	/// <summary>
	/// Verifies that the collapse and expand methods only act when the state would change, and that toggle always
	/// flips it.
	/// </summary>
	[Fact]
	public async Task CollapseExpandToggle_ChangeStateOnlyWhenNeeded()
	{
		var toggled = new List<bool>();
		var component = Render<PDSection>(parameters => parameters
			.Add(p => p.Toggled, (bool v) => toggled.Add(v)));
		var section = component.Instance;

		await component.InvokeAsync(section.ExpandAsync);
		toggled.Should().BeEmpty();

		await component.InvokeAsync(section.CollapseAsync);
		section.IsCollapsed.Should().BeTrue();

		await component.InvokeAsync(section.CollapseAsync);
		toggled.Should().Equal(true);

		await component.InvokeAsync(section.ExpandAsync);
		section.IsCollapsed.Should().BeFalse();

		await component.InvokeAsync(section.ToggleAsync);
		section.IsCollapsed.Should().BeTrue();

		toggled.Should().Equal(true, false, true);
	}
}
