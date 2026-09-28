using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDWidget"/>: its content types, header, styling, properties, refresh, and in-place configuration.
/// </summary>
public class PDWidgetTests : BunitContext
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
	/// Verifies that HTML content is sanitised before display, and aligned as asked.
	/// </summary>
	[Theory]
	[InlineData(ContentAlignment.Top, "")]
	[InlineData(ContentAlignment.Center, "pd-widget-align-center")]
	[InlineData(ContentAlignment.Bottom, "pd-widget-align-bottom")]
	public void HtmlContent_IsSanitisedAndAligned(ContentAlignment alignment, string alignmentClass)
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.ContentAlignment, alignment)
			.Add(x => x.Content, "<p class=\"safe\">Hello</p><script>alert(1)</script>"));

		var content = widget.Find(".pd-widget-html");
		content.QuerySelector(".safe")!.TextContent.Should().Be("Hello");
		content.QuerySelector("script").Should().BeNull();
		if (alignmentClass.Length > 0)
		{
			content.ClassList.Should().Contain(alignmentClass);
		}
	}

	/// <summary>
	/// Verifies that URL content is fetched through the delegate and shown sanitised.
	/// </summary>
	[Fact]
	public void UrlContent_IsFetchedAndShown()
	{
		var urls = new List<string>();
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Url)
			.Add(x => x.Content, "https://example.test/report")
			.Add(x => x.FetchContent, url => { urls.Add(url); return Task.FromResult("<em>Fetched</em>"); }));

		widget.WaitForAssertion(() => widget.Find(".pd-widget-url em").TextContent.Should().Be("Fetched"));
		urls.Should().AllBe("https://example.test/report");
	}

	/// <summary>
	/// Verifies that a failed fetch, or a missing fetch delegate, is shown as an error in the widget.
	/// </summary>
	[Theory]
	[InlineData(true, "Failed to load content: Offline")]
	[InlineData(false, "No FetchContent delegate provided.")]
	public void UrlContent_ProblemsAreShownAsErrors(bool withDelegate, string message)
	{
		Func<string, Task<string>>? fetch = withDelegate ? _ => throw new HttpRequestException("Offline") : null;

		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Url)
			.Add(x => x.Content, "https://example.test/report")
			.Add(x => x.FetchContent, fetch));

		widget.WaitForAssertion(() => widget.Find(".pd-widget-error small").TextContent.Should().Be(message));
	}

	/// <summary>
	/// Verifies that a URL widget with no URL fetches nothing and shows no error.
	/// </summary>
	[Fact]
	public void UrlContent_WithoutAUrl_FetchesNothing()
	{
		var calls = 0;
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Url)
			.Add(x => x.FetchContent, _ => { calls++; return Task.FromResult(string.Empty); }));

		widget.FindAll(".pd-widget-error").Should().BeEmpty();
		calls.Should().Be(0);
	}

	/// <summary>
	/// Verifies that the clock shows the time and date in the given time zone and formats.
	/// </summary>
	[Fact]
	public void Clock_ShowsTheTimeAndDateInTheGivenZoneAndFormat()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Clock)
			.Add(x => x.ClockTimeZone, TimeZoneInfo.Utc)
			.Add(x => x.ClockTimeFormat, "'time' yyyy")
			.Add(x => x.ClockDateFormat, "'date' yyyy"));

		var year = DateTime.UtcNow.Year;
		widget.WaitForAssertion(() => widget.Find(".pd-widget-clock-time").TextContent.Should().Be($"time {year}"));
		widget.Find(".pd-widget-clock-date").TextContent.Should().Be($"date {year}");
	}

	/// <summary>
	/// Verifies that a clock without a time zone shows local time.
	/// </summary>
	[Fact]
	public void Clock_WithoutAZone_ShowsLocalTime()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Clock)
			.Add(x => x.ClockTimeFormat, "%K"));

		var offset = DateTime.Now.ToString("%K", System.Globalization.CultureInfo.CurrentCulture);
		widget.WaitForAssertion(() => widget.Find(".pd-widget-clock-time").TextContent.Should().Be(offset));
	}

	/// <summary>
	/// Verifies that image bytes are shown as a data URI of the given type, in preference to a URL.
	/// </summary>
	[Fact]
	public void Image_FromBytes_IsADataUri()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Image)
			.Add(x => x.ContentBytes, [1, 2, 3])
			.Add(x => x.ImageMimeType, "image/gif")
			.Add(x => x.Content, "https://example.test/ignored.png"));

		widget.WaitForAssertion(() => widget.Find("img.pd-widget-img").GetAttribute("src").Should().Be("data:image/gif;base64,AQID"));
		widget.Find("img.pd-widget-img").GetAttribute("alt").Should().Be("Sales");
	}

	/// <summary>
	/// Verifies that an image URL is used when there are no bytes, and nothing is shown when there is neither.
	/// </summary>
	[Theory]
	[InlineData("https://example.test/logo.png", 1)]
	[InlineData(null, 0)]
	public void Image_FromAUrl_OrNothing(string? url, int images)
	{
		var widget = RenderWidget(p => p
			.Add(x => x.WidgetType, PDWidgetType.Image)
			.Add(x => x.Content, url));

		widget.WaitForAssertion(() => widget.FindAll("img").Should().HaveCount(images));
		if (url is not null)
		{
			widget.Find("img").GetAttribute("src").Should().Be(url);
		}
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

	private IRenderedComponent<PDWidget> RenderConfiguring(Action<ComponentParameterCollectionBuilder<PDWidget>>? more = null)
	{
		var widget = RenderWidget(p =>
		{
			p.Add(x => x.IsEditable, true);
			more?.Invoke(p);
		});
		widget.Find(".pd-widget-configure").Click();
		return widget;
	}

	/// <summary>
	/// Verifies that an editable widget offers a configure button that opens the panel, with nothing to save yet.
	/// </summary>
	[Fact]
	public void Configure_OpensThePanelWithNothingToSave()
	{
		var widget = RenderWidget(p => p.Add(x => x.IsEditable, true));
		widget.Find(".pd-widget-header").ClassList.Should().Contain("pd-widget-header-editable");

		widget.Find(".pd-widget-configure").Click();

		widget.FindAll(".pd-widget-config").Should().ContainSingle();
		ConfigSelect(widget, "Overflow").GetAttribute("value").Should().Be("hidden");
		ConfigButton(widget, "Save").HasAttribute("disabled").Should().BeTrue();
		widget.FindAll(".pd-widget-custom").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the overflow choice shown in the panel reflects the overflow parameters.
	/// </summary>
	[Theory]
	[InlineData(OverflowBehavior.Auto, OverflowBehavior.Hidden, "vertical")]
	[InlineData(OverflowBehavior.Hidden, OverflowBehavior.Auto, "horizontal")]
	[InlineData(OverflowBehavior.Auto, OverflowBehavior.Auto, "both")]
	public void Configure_ShowsTheCurrentOverflow(OverflowBehavior vertical, OverflowBehavior horizontal, string expected)
	{
		var widget = RenderConfiguring(p => p
			.Add(x => x.VerticalOverflow, vertical)
			.Add(x => x.HorizontalOverflow, horizontal));

		ConfigSelect(widget, "Overflow").GetAttribute("value").Should().Be(expected);
	}

	/// <summary>
	/// Verifies that choosing an overflow in the panel applies it to the body and enables saving.
	/// </summary>
	[Theory]
	[InlineData("vertical", "overflow-y: auto; overflow-x: hidden;")]
	[InlineData("horizontal", "overflow-y: hidden; overflow-x: auto;")]
	[InlineData("both", "overflow-y: auto; overflow-x: auto;")]
	public void ChoosingAnOverflow_AppliesItAndEnablesSave(string choice, string style)
	{
		var widget = RenderConfiguring();

		ConfigSelect(widget, "Overflow").Change(choice);

		widget.Find(".pd-widget-body").GetAttribute("style").Should().Be(style);
		ConfigButton(widget, "Save").HasAttribute("disabled").Should().BeFalse();
	}

	/// <summary>
	/// Verifies that choosing hidden overflow again leaves nothing to save.
	/// </summary>
	[Fact]
	public void ChoosingHiddenOverflow_LeavesTheBodyHidden()
	{
		var widget = RenderConfiguring(p => p.Add(x => x.VerticalOverflow, OverflowBehavior.Auto));

		ConfigSelect(widget, "Overflow").Change("hidden");

		widget.Find(".pd-widget-body").GetAttribute("style").Should().Be("overflow-y: hidden; overflow-x: hidden;");
	}

	/// <summary>
	/// Verifies that an alignment chosen in the panel is applied once saved, and an unknown one is ignored.
	/// </summary>
	[Fact]
	public void ChoosingAnAlignment_IsAppliedOnSave()
	{
		var widget = RenderConfiguring();

		ConfigSelect(widget, "Vertical Alignment").Change("Sideways");
		ConfigButton(widget, "Save").HasAttribute("disabled").Should().BeTrue();
		ConfigSelect(widget, "Vertical Alignment").Change("Bottom");
		ConfigButton(widget, "Save").Click();

		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.Find(".pd-widget-custom").ClassList.Should().Contain("pd-widget-align-bottom");
	}

	/// <summary>
	/// Verifies that choosing a new widget type raises <see cref="PDWidget.WidgetTypeChanged"/> and offers the matching editor.
	/// </summary>
	[Theory]
	[InlineData(PDWidgetType.Html, "HTML Content")]
	[InlineData(PDWidgetType.Url, "URL")]
	public void ChoosingAType_RaisesWidgetTypeChanged(PDWidgetType type, string editorLabel)
	{
		var types = new List<PDWidgetType>();
		var widget = RenderConfiguring(p => p.Add(x => x.WidgetTypeChanged, t => types.Add(t)));

		ConfigSelect(widget, "Widget Type").Change("Custom");
		ConfigSelect(widget, "Widget Type").Change("Nonsense");
		ConfigSelect(widget, "Widget Type").Change(type.ToString());

		types.Should().Equal(type);
		widget.Find(".pd-widget-config-editor-field label").TextContent.Should().Be(editorLabel);
		widget.FindComponents<PDMonacoEditor>().Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that changing away from a type with a running timer, and to a type that starts one, works in both directions.
	/// </summary>
	[Fact]
	public void ChangingBetweenTimedTypes_KeepsTheWidgetWorking()
	{
		var widget = RenderConfiguring(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.RefreshIntervalSeconds, 60)
			.Add(x => x.ClockTimeFormat, "'clock'"));

		ConfigSelect(widget, "Widget Type").Change("Clock");
		ConfigSelect(widget, "Widget Type").Change("Html");
		ConfigSelect(widget, "Widget Type").Change("Clock");
		ConfigButton(widget, "Save").Click();

		widget.WaitForAssertion(() => widget.Find(".pd-widget-clock-time").TextContent.Should().Be("clock"));
	}

	/// <summary>
	/// Verifies that content edited in the panel is shown and reported once saved.
	/// </summary>
	[Fact]
	public async Task EditedContent_IsShownAndReportedOnSave()
	{
		var reported = new List<string?>();
		var widget = RenderConfiguring(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.Content, "<p>Old</p>")
			.Add(x => x.ContentChanged, c => reported.Add(c)));
		var editor = widget.FindComponent<PDMonacoEditor>().Instance;

		await widget.InvokeAsync(() => editor.ValueChanged.InvokeAsync("<p class=\"new\">New</p>"));
		ConfigButton(widget, "Save").Click();

		reported.Should().Equal("<p class=\"new\">New</p>");
		widget.Find(".pd-widget-html .new").TextContent.Should().Be("New");
	}

	/// <summary>
	/// Verifies that saving without editing the content reports no content change.
	/// </summary>
	[Fact]
	public void Save_WithoutAContentEdit_ReportsNoContentChange()
	{
		var reported = new List<string?>();
		var widget = RenderConfiguring(p => p.Add(x => x.ContentChanged, c => reported.Add(c)));

		ConfigSelect(widget, "Overflow").Change("both");
		ConfigButton(widget, "Save").Click();

		reported.Should().BeEmpty();
		widget.FindAll(".pd-widget-config").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that cancelling with nothing changed closes the panel without asking.
	/// </summary>
	[Fact]
	public void Cancel_WithNoChanges_ClosesWithoutAsking()
	{
		var widget = RenderConfiguring();

		ConfigButton(widget, "Cancel").Click();

		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.FindAll(".modal").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that cancelling with changes asks first, and confirming puts back the type, content and overflow.
	/// </summary>
	[Fact]
	public async Task Cancel_WithChanges_AndConfirmed_RevertsEverything()
	{
		var widget = RenderConfiguring(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.Content, "<p class=\"old\">Old</p>"));
		var editor = widget.FindComponent<PDMonacoEditor>().Instance;
		await widget.InvokeAsync(() => editor.ValueChanged.InvokeAsync("<p>New</p>"));
		ConfigSelect(widget, "Overflow").Change("both");
		ConfigSelect(widget, "Widget Type").Change("Clock");

		var cancelling = ConfigButton(widget, "Cancel").ClickAsync(new MouseEventArgs());
		widget.FindAll(".modal button").Single(b => b.TextContent.Trim() == "Yes").Click();
		await cancelling;

		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.Find(".pd-widget-html .old").TextContent.Should().Be("Old");
		widget.Find(".pd-widget-body").GetAttribute("style").Should().Be("overflow-y: hidden; overflow-x: hidden;");
	}

	/// <summary>
	/// Verifies that declining to discard the changes keeps the panel open with the changes in place.
	/// </summary>
	[Fact]
	public async Task Cancel_WithChanges_AndDeclined_KeepsConfiguring()
	{
		var widget = RenderConfiguring();
		ConfigSelect(widget, "Overflow").Change("both");

		var cancelling = ConfigButton(widget, "Cancel").ClickAsync(new MouseEventArgs());
		widget.FindAll(".modal button").Single(b => b.TextContent.Trim() == "No").Click();
		await cancelling;

		widget.FindAll(".pd-widget-config").Should().ContainSingle();
		widget.Find(".pd-widget-body").GetAttribute("style").Should().Be("overflow-y: auto; overflow-x: auto;");
	}

	/// <summary>
	/// Verifies that holding the preview button shows the content in place of the panel until it is released.
	/// </summary>
	[Fact]
	public void Preview_ShowsTheContentWhileHeld()
	{
		var widget = RenderConfiguring(p => p.AddChildContent("<span class=\"mine\">Mine</span>"));

		ConfigButton(widget, "Preview").PointerLeave();
		widget.FindAll(".pd-widget-config").Should().ContainSingle();

		ConfigButton(widget, "Preview").PointerDown();
		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.Find(".mine").TextContent.Should().Be("Mine");

		ConfigButton(widget, "Preview").PointerUp();
		widget.FindAll(".pd-widget-config").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that the title cannot be renamed unless the panel is open.
	/// </summary>
	[Fact]
	public void Title_CannotBeRenamedOutsideConfiguration()
	{
		var widget = RenderWidget(p => p.Add(x => x.IsEditable, true));

		widget.Find(".pd-widget-title").Click();

		widget.FindAll(".pd-widget-title-input").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that while configuring the title can be renamed with Enter, raising <see cref="PDWidget.TitleChanged"/>.
	/// </summary>
	[Fact]
	public void Title_IsRenamedWithEnter()
	{
		var titles = new List<string>();
		var widget = RenderConfiguring(p => p.Add(x => x.TitleChanged, t => titles.Add(t)));

		widget.Find(".pd-widget-title").ClassList.Should().Contain("pd-widget-title-editable");
		widget.Find(".pd-widget-title").Click();
		var input = widget.Find(".pd-widget-title-input");
		input.GetAttribute("value").Should().Be("Sales");
		input.Input("  Revenue  ");
		widget.Find(".pd-widget-title-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		titles.Should().Equal("Revenue");
		widget.Find(".pd-widget-title").TextContent.Should().Be("Revenue");
	}

	/// <summary>
	/// Verifies that Escape abandons a rename, and a blank or unchanged title is not reported.
	/// </summary>
	[Fact]
	public void Title_EscapeBlankOrUnchanged_IsNotReported()
	{
		var titles = new List<string>();
		var widget = RenderConfiguring(p => p.Add(x => x.TitleChanged, t => titles.Add(t)));

		widget.Find(".pd-widget-title").Click();
		widget.Find(".pd-widget-title-input").Input("Revenue");
		widget.Find(".pd-widget-title-input").KeyDown(new KeyboardEventArgs { Key = "a" });
		widget.Find(".pd-widget-title-input").KeyDown(new KeyboardEventArgs { Key = "Escape" });
		widget.Find(".pd-widget-title").Click();
		widget.Find(".pd-widget-title-input").Input("   ");
		widget.Find(".pd-widget-title-input").Blur();
		widget.Find(".pd-widget-title").Click();
		widget.Find(".pd-widget-title-input").Blur();

		titles.Should().BeEmpty();
		widget.Find(".pd-widget-title").TextContent.Should().Be("Sales");
	}

	/// <summary>
	/// Verifies that the built-in edit button switches editing on, and switching it off closes an open panel.
	/// </summary>
	[Fact]
	public void EditButton_TogglesEditing()
	{
		var widget = RenderWidget(p => p.Add(x => x.ShowEditButton, true));
		var toggle = widget.Find(".pd-widget-edit-toggle");
		toggle.GetAttribute("title").Should().Be("Edit widget");

		toggle.Click();
		widget.Find(".pd-widget-edit-toggle").GetAttribute("title").Should().Be("Done editing");
		widget.Find(".pd-widget-configure").Click();
		widget.FindAll(".pd-widget-config").Should().ContainSingle();

		widget.Find(".pd-widget-edit-toggle").Click();
		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.FindAll(".pd-widget-configure").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a dashboard in edit mode makes its widgets editable, with no separate edit button.
	/// </summary>
	[Fact]
	public void DashboardEditMode_MakesTheWidgetEditable()
	{
		var widget = RenderWidget(p => p
			.Add(x => x.ShowEditButton, true)
			.AddCascadingValue("DashboardIsEditable", true));

		widget.FindAll(".pd-widget-configure").Should().ContainSingle();
		widget.FindAll(".pd-widget-edit-toggle").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that turning editing off from outside closes an open panel.
	/// </summary>
	[Fact]
	public void TurningEditingOff_ClosesThePanel()
	{
		var widget = RenderConfiguring();

		widget.Render(p => p.Add(x => x.IsEditable, false));

		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.FindAll(".pd-widget-configure").Should().BeEmpty();
	}
}
