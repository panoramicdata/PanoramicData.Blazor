using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Cancelling, previewing, renaming and edit-mode tests for <see cref="PDWidget"/>.
/// </summary>
public partial class PDWidgetTests
{
	/// <summary>
	/// Verifies that cancelling with nothing changed closes the panel without asking.
	/// </summary>
	[Fact]
	public async Task Cancel_WithNoChanges_ClosesWithoutAsking()
	{
		var widget = await RenderConfiguringAsync();

		await widget.InvokeAsync(() => ConfigButton(widget, "Cancel").ClickAsync(new MouseEventArgs()));

		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.FindAll(".modal").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that cancelling with changes asks first, and confirming puts back the type, content and overflow.
	/// </summary>
	[Fact]
	public async Task Cancel_WithChanges_AndConfirmed_RevertsEverything()
	{
		var widget = await RenderConfiguringAsync(p => p
			.Add(x => x.WidgetType, PDWidgetType.Html)
			.Add(x => x.Content, "<p class=\"old\">Old</p>"));
		var editor = widget.FindComponent<PDMonacoEditor>().Instance;
		await widget.InvokeAsync(() => editor.ValueChanged.InvokeAsync("<p>New</p>"));
		await widget.InvokeAsync(() => ConfigSelect(widget, "Overflow").ChangeAsync(new ChangeEventArgs { Value = "both" }));
		await widget.InvokeAsync(() => ConfigSelect(widget, "Widget Type").ChangeAsync(new ChangeEventArgs { Value = "Clock" }));

		var cancelling = widget.InvokeAsync(() => ConfigButton(widget, "Cancel").ClickAsync(new MouseEventArgs()));
		await widget.InvokeAsync(() => widget.FindAll(".modal button").Single(b => b.TextContent.Trim() == "Yes").ClickAsync(new MouseEventArgs()));
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
		var widget = await RenderConfiguringAsync();
		await widget.InvokeAsync(() => ConfigSelect(widget, "Overflow").ChangeAsync(new ChangeEventArgs { Value = "both" }));

		var cancelling = widget.InvokeAsync(() => ConfigButton(widget, "Cancel").ClickAsync(new MouseEventArgs()));
		await widget.InvokeAsync(() => widget.FindAll(".modal button").Single(b => b.TextContent.Trim() == "No").ClickAsync(new MouseEventArgs()));
		await cancelling;

		widget.FindAll(".pd-widget-config").Should().ContainSingle();
		widget.Find(".pd-widget-body").GetAttribute("style").Should().Be("overflow-y: auto; overflow-x: auto;");
	}

	/// <summary>
	/// Verifies that holding the preview button shows the content in place of the panel until it is released.
	/// </summary>
	[Fact]
	public async Task Preview_ShowsTheContentWhileHeld()
	{
		var widget = await RenderConfiguringAsync(p => p.AddChildContent("<span class=\"mine\">Mine</span>"));

		await widget.InvokeAsync(() => ConfigButton(widget, "Preview").PointerLeaveAsync(new PointerEventArgs()));
		widget.FindAll(".pd-widget-config").Should().ContainSingle();

		await widget.InvokeAsync(() => ConfigButton(widget, "Preview").PointerDownAsync(new PointerEventArgs()));
		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.Find(".mine").TextContent.Should().Be("Mine");

		await widget.InvokeAsync(() => ConfigButton(widget, "Preview").PointerUpAsync(new PointerEventArgs()));
		widget.FindAll(".pd-widget-config").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that the title cannot be renamed unless the panel is open.
	/// </summary>
	[Fact]
	public async Task Title_CannotBeRenamedOutsideConfiguration()
	{
		var widget = RenderWidget(p => p.Add(x => x.IsEditable, true));

		await widget.InvokeAsync(() => widget.Find(".pd-widget-title").ClickAsync(new MouseEventArgs()));

		widget.FindAll(".pd-widget-title-input").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that while configuring the title can be renamed with Enter, raising <see cref="PDWidget.TitleChanged"/>.
	/// </summary>
	[Fact]
	public async Task Title_IsRenamedWithEnter()
	{
		var titles = new List<string>();
		var widget = await RenderConfiguringAsync(p => p.Add(x => x.TitleChanged, t => titles.Add(t)));

		widget.Find(".pd-widget-title").ClassList.Should().Contain("pd-widget-title-editable");
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title").ClickAsync(new MouseEventArgs()));
		var input = widget.Find(".pd-widget-title-input");
		input.GetAttribute("value").Should().Be("Sales");
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title-input").InputAsync(new ChangeEventArgs { Value = "  Revenue  " }));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" }));

		titles.Should().Equal("Revenue");
		widget.Find(".pd-widget-title").TextContent.Should().Be("Revenue");
	}

	/// <summary>
	/// Verifies that Escape abandons a rename, and a blank or unchanged title is not reported.
	/// </summary>
	[Fact]
	public async Task Title_EscapeBlankOrUnchanged_IsNotReported()
	{
		var titles = new List<string>();
		var widget = await RenderConfiguringAsync(p => p.Add(x => x.TitleChanged, t => titles.Add(t)));

		await widget.InvokeAsync(() => widget.Find(".pd-widget-title").ClickAsync(new MouseEventArgs()));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title-input").InputAsync(new ChangeEventArgs { Value = "Revenue" }));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title-input").KeyDownAsync(new KeyboardEventArgs { Key = "a" }));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title-input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" }));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title").ClickAsync(new MouseEventArgs()));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title-input").InputAsync(new ChangeEventArgs { Value = "   " }));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title-input").BlurAsync(new FocusEventArgs()));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title").ClickAsync(new MouseEventArgs()));
		await widget.InvokeAsync(() => widget.Find(".pd-widget-title-input").BlurAsync(new FocusEventArgs()));

		titles.Should().BeEmpty();
		widget.Find(".pd-widget-title").TextContent.Should().Be("Sales");
	}

	/// <summary>
	/// Verifies that the built-in edit button switches editing on, and switching it off closes an open panel.
	/// </summary>
	[Fact]
	public async Task EditButton_TogglesEditing()
	{
		var widget = RenderWidget(p => p.Add(x => x.ShowEditButton, true));
		var toggle = widget.Find(".pd-widget-edit-toggle");
		toggle.GetAttribute("title").Should().Be("Edit widget");

		await widget.InvokeAsync(() => widget.Find(".pd-widget-edit-toggle").ClickAsync(new MouseEventArgs()));
		widget.Find(".pd-widget-edit-toggle").GetAttribute("title").Should().Be("Done editing");
		await widget.InvokeAsync(() => widget.Find(".pd-widget-configure").ClickAsync(new MouseEventArgs()));
		widget.FindAll(".pd-widget-config").Should().ContainSingle();

		await widget.InvokeAsync(() => widget.Find(".pd-widget-edit-toggle").ClickAsync(new MouseEventArgs()));
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
	public async Task TurningEditingOff_ClosesThePanel()
	{
		var widget = await RenderConfiguringAsync();

		widget.Render(p => p.Add(x => x.IsEditable, false));

		widget.FindAll(".pd-widget-config").Should().BeEmpty();
		widget.FindAll(".pd-widget-configure").Should().BeEmpty();
	}
}
