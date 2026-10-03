using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDWidget"/>: its content types, header, styling, properties, refresh, and in-place configuration.
/// </summary>
public partial class PDWidgetTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDWidgetTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDWidget> RenderWidget(Action<ComponentParameterCollectionBuilder<PDWidget>> parameters)
		=> Render<PDWidget>(p =>
		{
			p.Add(x => x.Id, "w1").Add(x => x.Title, "Sales");
			parameters(p);
		});

	private static AngleSharp.Dom.IElement ConfigButton(IRenderedComponent<PDWidget> widget, string text)
		=> widget.FindAll(".pd-widget-config-actions button").Single(b => b.TextContent.Trim() == text);

	private static AngleSharp.Dom.IElement ConfigSelect(IRenderedComponent<PDWidget> widget, string label)
		=> widget.FindAll(".pd-widget-config-field").Single(f => f.QuerySelector("label")!.TextContent == label).QuerySelector("select")!;

	/// <summary>
	/// Verifies that a hidden widget renders nothing at all.
	/// </summary>
	[Fact]
	public void Hidden_RendersNothing()
	{
		var widget = RenderWidget(p => p.Add(x => x.IsVisible, false));

		widget.Markup.Trim().Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the header shows the icon and title, and the root carries the id and both CSS parameters.
	/// </summary>
	[Fact]
	public void Header_ShowsTheIconAndTitle()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.Icon, "fas fa-chart-bar")
			.Add(x => x.CssClass, "outer")
			.Add(x => x.Css, "extra"));

		var root = widget.Find("div.pd-widget");
		root.Id.Should().Be("w1");
		root.ClassList.Should().Contain("outer").And.Contain("extra");
		widget.Find(".pd-widget-icon").ClassList.Should().Contain("fa-chart-bar");
		widget.Find(".pd-widget-title").TextContent.Should().Be("Sales");
		widget.FindAll(".pd-widget-configure").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the header is left out when titles are turned off or there is no title.
	/// </summary>
	[Theory]
	[InlineData(false, "Sales")]
	[InlineData(true, "")]
	public void Header_IsLeftOutWithoutATitle(bool showTitle, string title)
	{
		var widget = Render<PDWidget>(p => p
			.Add(x => x.ShowTitle, showTitle)
			.Add(x => x.Title, title));

		widget.FindAll(".pd-widget-header").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the widget's own header, border and content classes apply, overriding the dashboard's.
	/// </summary>
	[Fact]
	public void WidgetCss_OverridesTheDashboardCss()
	{
		var widget = RenderWidget(p => p
			.AddCascadingValue("DashboardWidgetHeaderCss", "dash-header")
			.AddCascadingValue("DashboardWidgetBorderCss", "dash-border")
			.AddCascadingValue("DashboardWidgetContentCss", "dash-content")
			.Add(x => x.HeaderCss, "own-header"));

		widget.Find(".pd-widget-header").ClassList.Should().Contain("own-header").And.NotContain("dash-header");
		widget.Find(".card").ClassList.Should().Contain("dash-border");
		widget.Find(".pd-widget-body").ClassList.Should().Contain("dash-content");
	}

	/// <summary>
	/// Verifies that the body's overflow style follows the overflow parameters.
	/// </summary>
	[Fact]
	public void Body_OverflowFollowsTheParameters()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.VerticalOverflow, OverflowBehavior.Auto)
			.Add(x => x.HorizontalOverflow, OverflowBehavior.Scroll));

		widget.Find(".pd-widget-body").GetAttribute("style").Should().Be("overflow-y: auto; overflow-x: scroll;");
	}

	/// <summary>
	/// Verifies that a custom widget renders its child content.
	/// </summary>
	[Fact]
	public void Custom_RendersTheChildContent()
	{
		var widget = RenderWidget(p => p.AddChildContent("<span class=\"mine\">Mine</span>"));

		widget.Find(".pd-widget-custom .mine").TextContent.Should().Be("Mine");
	}

	/// <summary>
	/// Verifies that a widget property is found on the widget first, then the dashboard, and otherwise is null.
	/// </summary>
	[Fact]
	public void GetProperty_PrefersTheWidgetThenTheDashboard()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.Properties, new Dictionary<string, string> { ["region"] = "EMEA" })
			.AddCascadingValue("DashboardProperties", new Dictionary<string, string> { ["region"] = "Global", ["currency"] = "GBP" }));

		widget.Instance.GetProperty("region").Should().Be("EMEA");
		widget.Instance.GetProperty("currency").Should().Be("GBP");
		widget.Instance.GetProperty("missing").Should().BeNull();
	}

	/// <summary>
	/// Verifies that a property lookup with no properties anywhere is null.
	/// </summary>
	[Fact]
	public void GetProperty_WithNoProperties_IsNull()
	{
		var widget = RenderWidget(_ => { });

		widget.Instance.GetProperty("region").Should().BeNull();
	}

	/// <summary>
	/// Verifies that a manual refresh reloads the content and raises <see cref="PDWidget.OnRefresh"/>.
	/// </summary>
	[Fact]
	public async Task Refresh_ReloadsAndRaisesOnRefresh()
	{
		var fetches = 0;
		var refreshed = 0;
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Url)
			.Add(x => x.Content, "https://example.test")
			.Add(x => x.FetchContent, _ => Task.FromResult($"<i>{++fetches}</i>"))
			.Add(x => x.OnRefresh, () => refreshed++));
		widget.WaitForAssertion(() => widget.FindAll(".pd-widget-url i").Should().ContainSingle());
		var before = fetches;

		await widget.InvokeAsync(widget.Instance.RefreshAsync);

		fetches.Should().Be(before + 1);
		refreshed.Should().Be(1);
		widget.Find(".pd-widget-url i").TextContent.Should().Be($"{before + 1}");
	}

	/// <summary>
	/// Verifies that a refresh interval reloads the content on its own and raises <see cref="PDWidget.OnRefresh"/>.
	/// </summary>
	[Fact]
	public void RefreshInterval_ReloadsOnItsOwn()
	{
		var refreshed = 0;
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.Content, "<p>Hi</p>")
			.Add(x => x.RefreshIntervalSeconds, 1)
			.Add(x => x.OnRefresh, () => refreshed++));

		widget.WaitForAssertion(() => refreshed.Should().BePositive(), TimeSpan.FromSeconds(10));
	}
}
