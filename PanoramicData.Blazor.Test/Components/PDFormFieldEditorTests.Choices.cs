using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Date, option list, check box and enum editor tests for <see cref="PDFormFieldEditor{TItem}"/>.
/// </summary>
public partial class PDFormFieldEditorTests
{
	/// <summary>A DateTime field is edited with the date time editor, showing time when asked.</summary>
	[Fact]
	public void DateTimeField_UsesTheDateTimeEditor()
	{
		var field = FieldFor(p => p.Born);
		field.DisplayOptions = new FieldDateTimeOptions { ShowTime = true, TimeStepSecs = 60 };
		var editor = RenderEditor(field);

		editor.Find(".pddatetime input.time").GetAttribute("step").Should().Be("60");
		editor.Find(".pddatetime input.date").Input("2000-02-03");

		editor.Instance.Form.Delta["Born"].Should().Be(new DateTime(2000, 2, 3, 10, 30, 0));
	}

	/// <summary>A DateTimeOffset field is edited with the offset editor, showing the offset when asked.</summary>
	[Fact]
	public void DateTimeOffsetField_UsesTheOffsetEditor()
	{
		var field = FieldFor(p => p.Seen);
		field.DisplayOptions = new FieldDateTimeOptions { ShowTime = true, ShowOffset = true };
		var editor = RenderEditor(field);

		editor.FindAll(".pddatetimeoffset select.offset").Should().ContainSingle();
		editor.Find(".pddatetimeoffset input.date").Input("2001-01-01");

		editor.Instance.Form.Delta["Seen"].Should().Be(new DateTimeOffset(2001, 1, 1, 9, 0, 0, TimeSpan.FromHours(1)));
	}

	/// <summary>Date fields without date options show no time.</summary>
	[Fact]
	public void DateFields_WithoutOptions_ShowNoTime()
	{
		var dateTime = RenderEditor(FieldFor(p => p.Born));
		var offset = RenderEditor(FieldFor(p => p.Seen));

		dateTime.FindAll("input.time").Should().BeEmpty();
		offset.FindAll("input.time").Should().BeEmpty();
	}

	/// <summary>A field with options renders a select with the current value chosen, and a choice writes the delta.</summary>
	[Fact]
	public void OptionsField_RendersASelect()
	{
		var field = FieldFor(p => p.Name);
		field.Options = (_, _) =>
		[
			new OptionInfo { Text = "Ada", Value = "Ada" },
			new OptionInfo { Text = "Grace", Value = "Grace" },
			new OptionInfo { Text = "Retired", Value = "Retired", IsDisabled = true }
		];
		var editor = RenderEditor(field);

		var options = editor.FindAll("select.form-select option");
		options.Should().HaveCount(3);
		options[0].HasAttribute("selected").Should().BeTrue();
		options[2].HasAttribute("disabled").Should().BeTrue();

		editor.Find("select").Input("Grace");
		editor.Instance.Form.Delta["Name"].Should().Be("Grace");
	}

	/// <summary>A select input with no value writes nothing.</summary>
	[Fact]
	public async Task OnSelectInputChanged_WithNoValue_WritesNothing()
	{
		var field = FieldFor(p => p.Name);
		var editor = RenderEditor(field);

		await editor.InvokeAsync(() => editor.Instance.OnSelectInputChanged(new ChangeEventArgs { Value = null }, field));

		editor.Instance.Form.Delta.Should().BeEmpty();
	}

	/// <summary>A boolean field uses a check box by default; clicking it writes the delta.</summary>
	[Fact]
	public void BooleanField_UsesACheckBox()
	{
		var field = FieldFor(p => p.Active);
		field.Label = "Active";
		var editor = RenderEditor(field);

		editor.Find(".pdformcheckbox .label").TextContent.Should().Be("Active");
		editor.Find(".pdformcheckbox i").Click();

		editor.Instance.Form.Delta["Active"].Should().Be(false);
	}

	/// <summary>A boolean field with the toggle switch option uses a toggle switch; clicking it writes the delta.</summary>
	[Fact]
	public void BooleanField_WithToggleSwitchOption_UsesAToggleSwitch()
	{
		var field = FieldFor(p => p.Active);
		field.DisplayOptions = new FieldBooleanOptions { Style = FieldBooleanOptions.DisplayComponent.ToggleSwitch, CssClass = "sw", OnText = "Yes", OffText = "No" };
		var editor = RenderEditor(field);

		editor.Find(".pdtoggleswitch").ClassList.Should().Contain("sw");
		editor.Find(".pdtoggleswitch text").TextContent.Should().Be("Yes");
		editor.Find(".pdtoggleswitch svg").Click();

		editor.Instance.Form.Delta["Active"].Should().Be(false);
	}

	/// <summary>An enum field lists display names with the current value selected; choosing one writes the delta.</summary>
	[Fact]
	public void EnumField_ListsDisplayNames()
	{
		var editor = RenderEditor(FieldFor(p => p.Favourite));

		var options = editor.FindAll("select option");
		options.Select(o => o.TextContent.Trim()).Should().Equal("Red", "Sea green", "Blue");
		options.Single(o => o.HasAttribute("selected")).GetAttribute("value").Should().Be("SeaGreen");

		editor.Find("select").Input("Blue");
		editor.Instance.Form.Delta["Favourite"].Should().Be(Colour.Blue);
	}

	/// <summary>An enum field that is not a property offers no options, as its values cannot be written back.</summary>
	[Fact]
	public void EnumField_NotAProperty_ListsNoOptions()
	{
		var editor = RenderEditor(FieldFor(p => p.FavouriteField));

		editor.Find("select").Should().NotBeNull();
		editor.FindAll("select option").Should().BeEmpty();
	}
}
