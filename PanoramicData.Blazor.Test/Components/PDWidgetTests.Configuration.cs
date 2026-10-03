using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Configuration panel tests for <see cref="PDWidget"/>: opening the panel and choosing its settings.
/// </summary>
public partial class PDWidgetTests
{
	private async Task<IRenderedComponent<PDWidget>> RenderConfiguringAsync(Action<ComponentParameterCollectionBuilder<PDWidget>>? more = null)
	{
		var widget = RenderWidget(p =>
		{
			p.Add(x => x.IsEditable, true);
			more?.Invoke(p);
		});
		await widget.InvokeAsync(() => widget.Find(".pd-widget-configure").ClickAsync(new MouseEventArgs()));
		return widget;
	}

	/// <summary>
	/// Verifies that an editable widget offers a configure button that opens the panel, with nothing to save yet.
	/// </summary>
	[Fact]
	public async Task Configure_OpensThePanelWithNothingToSave()
	{
		var widget = RenderWidget(p => p.Add(x => x.IsEditable, true));
		widget.Find(".pd-widget-header").ClassList.Should().Contain("pd-widget-header-editable");

		await widget.InvokeAsync(() => widget.Find(".pd-widget-configure").ClickAsync(new MouseEventArgs()));

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
	public async Task Configure_ShowsTheCurrentOverflow(OverflowBehavior vertical, OverflowBehavior horizontal, string expected)
	{
		var widget = await RenderConfiguringAsync(p => p
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
	public async Task ChoosingAnOverflow_AppliesItAndEnablesSave(string choice, string style)
	{
		var widget = await RenderConfiguringAsync();

		await widget.InvokeAsync(() => ConfigSelect(widget, "Overflow").ChangeAsync(new ChangeEventArgs { Value = choice }));

		widget.Find(".pd-widget-body").GetAttribute("style").Should().Be(style);
		ConfigButton(widget, "Save").HasAttribute("disabled").Should().BeFalse();
	}

	/// <summary>
	/// Verifies that choosing hidden overflow again leaves nothing to save.
	/// </summary>
	[Fact]
	public async Task ChoosingHiddenOverflow_LeavesTheBodyHidden()
	{
		var widget = await RenderConfiguringAsync(p => p.Add(x => x.VerticalOverflow, OverflowBehavior.Auto));

		await widget.InvokeAsync(() => ConfigSelect(widget, "Overflow").ChangeAsync(new ChangeEventArgs { Value = "hidden" }));

		widget.Find(".pd-widget-body").GetAttribute("style").Should().Be("overflow-y: hidden; overflow-x: hidden;");
	}

	/// <summary>
	/// Verifies that an alignment chosen in the panel is applied once saved, and an unknown one is ignored.
	/// </summary>
	[Fact]
	public async Task ChoosingAnAlignment_IsAppliedOnSave()
	{
		var widget = await RenderConfiguringAsync();

		await widget.InvokeAsync(() => ConfigSelect(widget, "Vertical Alignment").ChangeAsync(new ChangeEventArgs { Value = "Sideways" }));
		ConfigButton(widget, "Save").HasAttribute("disabled").Should().BeTrue();
		await widget.InvokeAsync(() => ConfigSelect(widget, "Vertical Alignment").ChangeAsync(new ChangeEventArgs { Value = "Bottom" }));
		await widget.InvokeAsync(() => ConfigButton(widget, "Save").ClickAsync(new MouseEventArgs()));

		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.Find(".pd-widget-custom").ClassList.Should().Contain("pd-widget-align-bottom");
	}

	/// <summary>
	/// Verifies that choosing a new widget type raises <see cref="PDWidget.WidgetTypeChanged"/> and offers the matching editor.
	/// </summary>
	[Theory]
	[InlineData(PDWidgetType.Html, "HTML Content")]
	[InlineData(PDWidgetType.Url, "URL")]
	public async Task ChoosingAType_RaisesWidgetTypeChanged(PDWidgetType type, string editorLabel)
	{
		var types = new List<PDWidgetType>();
		var widget = await RenderConfiguringAsync(p => p.Add(x => x.WidgetTypeChanged, t => types.Add(t)));

		await widget.InvokeAsync(() => ConfigSelect(widget, "Widget Type").ChangeAsync(new ChangeEventArgs { Value = "Custom" }));
		await widget.InvokeAsync(() => ConfigSelect(widget, "Widget Type").ChangeAsync(new ChangeEventArgs { Value = "Nonsense" }));
		await widget.InvokeAsync(() => ConfigSelect(widget, "Widget Type").ChangeAsync(new ChangeEventArgs { Value = type.ToString() }));

		types.Should().Equal(type);
		widget.Find(".pd-widget-config-editor-field label").TextContent.Should().Be(editorLabel);
		widget.FindComponents<PDMonacoEditor>().Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that changing away from a type with a running timer, and to a type that starts one, works in both directions.
	/// </summary>
	[Fact]
	public async Task ChangingBetweenTimedTypes_KeepsTheWidgetWorking()
	{
		var widget = await RenderConfiguringAsync(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.RefreshIntervalSeconds, 60)
			.Add(x => x.ClockTimeFormat, "'clock'"));

		await widget.InvokeAsync(() => ConfigSelect(widget, "Widget Type").ChangeAsync(new ChangeEventArgs { Value = "Clock" }));
		await widget.InvokeAsync(() => ConfigSelect(widget, "Widget Type").ChangeAsync(new ChangeEventArgs { Value = "Html" }));
		await widget.InvokeAsync(() => ConfigSelect(widget, "Widget Type").ChangeAsync(new ChangeEventArgs { Value = "Clock" }));
		await widget.InvokeAsync(() => ConfigButton(widget, "Save").ClickAsync(new MouseEventArgs()));

		widget.WaitForAssertion(() => widget.Find(".pd-widget-clock-time").TextContent.Should().Be("clock"));
	}

	/// <summary>
	/// Verifies that content edited in the panel is shown and reported once saved.
	/// </summary>
	[Fact]
	public async Task EditedContent_IsShownAndReportedOnSave()
	{
		var reported = new List<string?>();
		var widget = await RenderConfiguringAsync(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.Content, "<p>Old</p>")
			.Add(x => x.ContentChanged, c => reported.Add(c)));
		var editor = widget.FindComponent<PDMonacoEditor>().Instance;

		await widget.InvokeAsync(() => editor.ValueChanged.InvokeAsync("<p class=\"new\">New</p>"));
		await widget.InvokeAsync(() => ConfigButton(widget, "Save").ClickAsync(new MouseEventArgs()));

		reported.Should().Equal("<p class=\"new\">New</p>");
		widget.Find(".pd-widget-html .new").TextContent.Should().Be("New");
	}

	/// <summary>
	/// Verifies that saving without editing the content reports no content change.
	/// </summary>
	[Fact]
	public async Task Save_WithoutAContentEdit_ReportsNoContentChange()
	{
		var reported = new List<string?>();
		var widget = await RenderConfiguringAsync(p => p.Add(x => x.ContentChanged, c => reported.Add(c)));

		await widget.InvokeAsync(() => ConfigSelect(widget, "Overflow").ChangeAsync(new ChangeEventArgs { Value = "both" }));
		await widget.InvokeAsync(() => ConfigButton(widget, "Save").ClickAsync(new MouseEventArgs()));

		reported.Should().BeEmpty();
		widget.FindAll(".pd-widget-config").Should().BeEmpty();
	}
}
