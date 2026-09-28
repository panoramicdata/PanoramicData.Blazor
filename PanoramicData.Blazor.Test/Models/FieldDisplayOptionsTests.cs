using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="FieldDisplayOptions"/> and the option records derived from it.</summary>
public class FieldDisplayOptionsTests
{
	/// <summary>The base options disallow nulls, have no css class and a width weight of one.</summary>
	[Fact]
	public void FieldDisplayOptions_HasDefaults()
	{
		var options = new FieldDisplayOptions();

		options.AllowNulls.Should().BeFalse();
		options.CssClass.Should().BeEmpty();
		options.WidthWeight.Should().Be(1);
	}

	/// <summary>The base options are a record, so equality is by value.</summary>
	[Fact]
	public void FieldDisplayOptions_EqualityIsByValue()
	{
		var options = new FieldDisplayOptions { CssClass = "wide", WidthWeight = 2, AllowNulls = true };

		options.Should().Be(new FieldDisplayOptions { CssClass = "wide", WidthWeight = 2, AllowNulls = true });
		options.Should().NotBe(options with { WidthWeight = 3 });
	}

	/// <summary>Boolean options default to a rounded checkbox with the label after it and no on/off text.</summary>
	[Fact]
	public void FieldBooleanOptions_HasDefaults()
	{
		var options = new FieldBooleanOptions();

		options.Style.Should().Be(FieldBooleanOptions.DisplayComponent.Checkbox);
		options.LabelBefore.Should().BeFalse();
		options.Rounded.Should().BeTrue();
		options.OnText.Should().BeEmpty();
		options.OffText.Should().BeEmpty();
	}

	/// <summary>Boolean options round-trip, including the inherited members.</summary>
	[Fact]
	public void FieldBooleanOptions_RoundTrip()
	{
		var options = new FieldBooleanOptions
		{
			Style = FieldBooleanOptions.DisplayComponent.ToggleSwitch,
			LabelBefore = true,
			Rounded = false,
			OnText = "Yes",
			OffText = "No",
			CssClass = "flag"
		};

		options.Style.Should().Be(FieldBooleanOptions.DisplayComponent.ToggleSwitch);
		options.LabelBefore.Should().BeTrue();
		options.Rounded.Should().BeFalse();
		options.OnText.Should().Be("Yes");
		options.OffText.Should().Be("No");
		options.CssClass.Should().Be("flag");
	}

	/// <summary>Date/time options default to a date-only editor with a one second step.</summary>
	[Fact]
	public void FieldDateTimeOptions_HasDefaults()
	{
		var options = new FieldDateTimeOptions();

		options.ShowOffset.Should().BeFalse();
		options.ShowTime.Should().BeFalse();
		options.TimeStepSecs.Should().Be(1);
	}

	/// <summary>Date/time options round-trip.</summary>
	[Fact]
	public void FieldDateTimeOptions_RoundTrip()
	{
		var options = new FieldDateTimeOptions { ShowOffset = true, ShowTime = true, TimeStepSecs = 60 };

		options.ShowOffset.Should().BeTrue();
		options.ShowTime.Should().BeTrue();
		options.TimeStepSecs.Should().Be(60);
	}

	/// <summary>String options default to a four row, non-resizing text box with no code language.</summary>
	[Fact]
	public void FieldStringOptions_HasDefaults()
	{
		var options = new FieldStringOptions();

		options.CssClass.Should().BeEmpty();
		options.CodeLanguage.Should().BeEmpty();
		options.Editor.Should().Be(FieldStringOptions.Editors.TextBox);
		options.Resize.Should().BeFalse();
		options.ResizeCssCls.Should().BeEmpty();
		options.Rows.Should().Be(4);
		options.MonacoOptions.Should().NotBeNull();
	}

	/// <summary>The default Monaco options make the editor read-only.</summary>
	[Fact]
	public void FieldStringOptions_DefaultMonacoOptionsAreReadOnly()
	{
		var construction = new FieldStringOptions().MonacoOptions(null!);

		construction.ReadOnly.Should().BeTrue();
	}

	/// <summary>String options round-trip.</summary>
	[Fact]
	public void FieldStringOptions_RoundTrip()
	{
		var options = new FieldStringOptions
		{
			CodeLanguage = "sql",
			Editor = FieldStringOptions.Editors.Monaco,
			Resize = true,
			ResizeCssCls = "resize-v",
			Rows = 10
		};

		options.CodeLanguage.Should().Be("sql");
		options.Editor.Should().Be(FieldStringOptions.Editors.Monaco);
		options.Resize.Should().BeTrue();
		options.ResizeCssCls.Should().Be("resize-v");
		options.Rows.Should().Be(10);
	}
}
