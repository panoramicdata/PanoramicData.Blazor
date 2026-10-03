using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Numeric and nullable field editor tests for <see cref="PDFormFieldEditor{TItem}"/>.
/// </summary>
public partial class PDFormFieldEditorTests
{
	/// <summary>A numeric field uses a number input with its limits, and a change is converted to the field's type.</summary>
	[Fact]
	public void NumericField_UsesANumberInput()
	{
		var field = FieldFor(p => p.Age);
		field.MinValue = 0;
		field.MaxValue = 150;
		var editor = RenderEditor(field);

		var input = editor.Find("input[type=number]");
		input.GetAttribute("min").Should().Be("0");
		input.GetAttribute("max").Should().Be("150");
		input.GetAttribute("value").Should().Be("36");
		input.Change("42");

		editor.Instance.Form.Delta["Age"].Should().Be(42);
	}

	/// <summary>A numeric input that cannot be converted is ignored.</summary>
	[Fact]
	public void NumericField_WithAnUnreadableValue_IsIgnored()
	{
		var editor = RenderEditor(FieldFor(p => p.Age));

		editor.Find("input[type=number]").Change("forty");

		editor.Instance.Form.Delta.Should().BeEmpty();
	}

	/// <summary>A nullable numeric field converts text to its underlying type.</summary>
	[Fact]
	public void NullableNumericField_ConvertsText()
	{
		var editor = RenderEditor(FieldFor(p => p.Score!));

		editor.Find("input[type=number]").Change("5");

		editor.Instance.Form.Delta["Score"].Should().Be(5);
	}

	/// <summary>A nullable field allowing nulls shows a has-value check box and is read-only while null.</summary>
	[Fact]
	public void NullableField_AllowingNulls_IsReadOnlyWhileNull()
	{
		var field = FieldFor(p => p.Score!);
		field.DisplayOptions = new FieldDisplayOptions { AllowNulls = true };

		var editor = RenderEditor(field);

		editor.Find(".ms-nullable i").ClassList.Should().Contain("fa-square");
		editor.Find("input[type=number]").HasAttribute("disabled").Should().BeTrue();
		editor.Instance.IsReadOnly(field).Should().BeTrue();
	}

	/// <summary>Ticking the has-value box gives the field its type's default; unticking sets it back to null.</summary>
	[Fact]
	public void HasValueCheckBox_SetsADefaultThenNull()
	{
		var field = FieldFor(p => p.Score!);
		field.DisplayOptions = new FieldDisplayOptions { AllowNulls = true };
		var editor = RenderEditor(field);

		editor.Find(".ms-nullable i").Click();
		editor.Instance.Form.Delta["Score"].Should().Be(0);
		editor.Find("input[type=number]").HasAttribute("disabled").Should().BeFalse();

		editor.Find(".ms-nullable i").Click();
		editor.Instance.Form.Delta.Should().NotContainKey("Score");
		editor.Find("input[type=number]").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Ticking the has-value box on a nullable field of each type sets that type's default.</summary>
	[Theory]
	[InlineData(nameof(Person.Nickname), "")]
	[InlineData(nameof(Person.Verified), false)]
	public void HasValueCheckBox_UsesTheTypesDefault(string property, object expected)
	{
		var field = property == nameof(Person.Nickname) ? FieldFor(p => p.Nickname!) : FieldFor(p => p.Verified!);
		field.DisplayOptions = new FieldDisplayOptions { AllowNulls = true };
		var editor = RenderEditor(field);

		editor.Find(".ms-nullable i").Click();

		editor.Instance.Form.Delta[property].Should().Be(expected);
	}

	/// <summary>Ticking the has-value box on a nullable date or Guid sets today or an empty Guid.</summary>
	[Fact]
	public void HasValueCheckBox_OnDateAndGuidFields_SetsTheirDefaults()
	{
		var date = FieldFor(p => p.Due!);
		date.DisplayOptions = new FieldDisplayOptions { AllowNulls = true };
		var guid = FieldFor(p => p.Token!);
		guid.DisplayOptions = new FieldDisplayOptions { AllowNulls = true };
		var form = RenderForm().Instance;
		var dateEditor = RenderEditor(date, form);
		var guidEditor = RenderEditor(guid, form);

		dateEditor.Find(".ms-nullable i").Click();
		guidEditor.Find(".ms-nullable i").Click();

		form.Delta["Due"].Should().Be(DateTime.Today);
		form.Delta["Token"].Should().Be(Guid.Empty);
	}
}
