using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The classes <see cref="PDFormBody{TItem}"/> gives a host to colour its validation state and helper icons by.
/// </summary>
public partial class PDFormBodyTests
{
	/// <summary>A label keeps alert-danger and says whether the field is still waiting for a value or holds a wrong one.</summary>
	/// <param name="suppressed">Whether the field's errors are suppressed, as they are until the user edits it.</param>
	/// <param name="stateClass">The state class the label should have.</param>
	/// <param name="otherClass">The state class it should not have.</param>
	[Theory]
	[InlineData(true, "pd-form-title-required", "pd-form-title-invalid")]
	[InlineData(false, "pd-form-title-invalid", "pd-form-title-required")]
	public async Task A_label_in_error_says_which_state_it_is_in(bool suppressed, string stateClass, string otherClass)
	{
		var host = RenderForm([Field(x => x.Name)]);
		await Edit(host, FormModes.Edit);
		var form = FormOf(host);

		await host.InvokeAsync(() =>
		{
			form.Fields[0].SuppressErrors = suppressed;
			form.SetFieldErrors(nameof(Person.Name), "Required");
		});
		host.Render();

		host.Find(".title-box").ClassList.Should().Contain("alert-danger").And.Contain(stateClass).And.NotContain(otherClass);
	}

	/// <summary>A valid field's label has no state class, and its validation indicator has the status class.</summary>
	[Fact]
	public async Task A_valid_label_has_no_state_class_and_the_indicator_has_the_status_class()
	{
		var host = RenderForm([Field(x => x.Name)]);

		await Edit(host, FormModes.Edit);

		host.Find(".title-box").ClassList.Should().NotContain("pd-form-title-required").And.NotContain("pd-form-title-invalid");
		host.Find(".d-table.input-group-text").ClassList.Should().Contain("pd-form-status").And.Contain("alert-success");
	}

	/// <summary>The help link, helper and copy icons, and a field's error message, carry the library's classes rather than Bootstrap's fixed colours.</summary>
	[Fact]
	public async Task Helper_icons_and_messages_use_themable_classes()
	{
		var helper = new FormFieldHelper<Person> { IconCssClass = "fa-helper", ToolTip = "Pick" };
		var host = RenderForm([Field(x => x.Name, extra: new()
		{
			[nameof(PDField<Person>.HelpUrl)] = "https://help/",
			[nameof(PDField<Person>.Helper)] = helper,
			[nameof(PDField<Person>.ShowCopyButton)] = (Func<Person?, bool>)(_ => true)
		})]);
		await Edit(host, FormModes.Edit);
		var form = FormOf(host);

		await host.InvokeAsync(() =>
		{
			form.Fields[0].SuppressErrors = false;
			form.SetFieldErrors(nameof(Person.Name), "Too short");
		});
		host.Render();

		host.Find(".fa-external-link-alt").ClassList.Should().Contain("pd-form-helper").And.NotContain("text-info");
		host.Find("i.fa-helper").ClassList.Should().Contain("pd-form-helper").And.NotContain("text-info");
		host.Find(".pdClipboard-button").ClassList.Should().Contain("pd-form-helper").And.NotContain("text-info");
		host.Find(".pd-form-field-form-error-message").ClassList.Should().NotContain("text-danger");
	}
}
