using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that a nullable numeric field edited in <see cref="PDFormFieldEditor{TItem}"/> can be cleared back to
/// null, while a non-nullable one still ignores an empty entry (#167).
/// </summary>
public partial class PDFormFieldEditorTests
{
	/// <summary>Clearing the number input of a nullable field that has a value records null.</summary>
	[Theory]
	[InlineData("")]
	[InlineData(null)]
	public async Task NullableNumericField_Cleared_RecordsNull(string? cleared)
	{
		_person.Score = 3;
		var editor = RenderEditor(FieldFor(p => p.Score!));

		await editor.Find("input[type=number]").ChangeAsync(new ChangeEventArgs { Value = cleared });

		editor.Instance.Form.Delta.Should().ContainKey("Score").WhoseValue.Should().BeNull();
	}

	/// <summary>Typing a number into a nullable field and then clearing it returns the field to null.</summary>
	[Fact]
	public async Task NullableNumericField_SetThenCleared_ReturnsToNull()
	{
		var field = FieldFor(p => p.Score!);
		var editor = RenderEditor(field);

		await editor.Find("input[type=number]").ChangeAsync(new ChangeEventArgs { Value = "5" });
		editor.Instance.Form.Delta["Score"].Should().Be(5);
		await editor.Find("input[type=number]").ChangeAsync(new ChangeEventArgs { Value = "" });

		editor.Instance.Form.Delta.Should().NotContainKey("Score", "null is the original value, so nothing is changed");
		editor.Instance.Form.GetFieldValue(field).Should().BeNull();
	}

	/// <summary>Clearing the number input of a non-nullable field is still ignored, since it cannot hold null.</summary>
	[Fact]
	public async Task NonNullableNumericField_Cleared_IsIgnored()
	{
		var editor = RenderEditor(FieldFor(p => p.Age));

		await editor.Find("input[type=number]").ChangeAsync(new ChangeEventArgs { Value = "" });

		editor.Instance.Form.Delta.Should().BeEmpty();
	}
}
