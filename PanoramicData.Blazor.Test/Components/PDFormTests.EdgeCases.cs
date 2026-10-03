using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Edge cases of <see cref="PDForm{TItem}"/> validation and of building the item with its changes applied.
/// </summary>
public partial class PDFormTests
{
	/// <summary>Validating a field without a value validates its latest value.</summary>
	[Fact]
	public async Task ValidateFieldAsync_WithoutAValue_ValidatesTheLatestValue()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit, validate: false);
		form.Instance.Delta["Name"] = string.Empty;

		var typed = await form.InvokeAsync(() => form.Instance.ValidateFieldAsync(field));

		typed.Should().Be(string.Empty);
		form.Instance.Errors["Name"].Should().Equal("The Full name field is required.");
	}

	/// <summary>Custom validation removing a message the field does not have leaves the errors as they were.</summary>
	[Fact]
	public async Task CustomValidate_RemovingAnAbsentMessage_ChangesNothing()
	{
		var form = RenderForm(p => p.Add(x => x.CustomValidate, args => args.RemoveErrorMessages.Add("Age", "Not an error.")));
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = string.Empty }, FormModes.Edit, validate: false);

		await form.InvokeAsync(() => form.Instance.ValidateFieldAsync(field));

		form.Instance.Errors.Keys.Should().Equal("Name");
	}

	/// <summary>A change the item cannot take leaves no item with updates.</summary>
	[Fact]
	public async Task GetItemWithUpdates_ChangeOfTheWrongType_ReturnsNull()
	{
		var form = RenderForm();
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit, validate: false);
		form.Instance.Delta["Age"] = "not a number";

		form.Instance.GetItemWithUpdates().Should().BeNull();
	}
}
