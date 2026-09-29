using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Save and delete tests for <see cref="PDForm{TItem}"/>.
/// </summary>
public partial class PDFormTests
{
	/// <summary>
	/// Verifies that a successful create applies the delta, calls the provider, raises Created and hides
	/// the form, or moves it to edit mode when it is not hidden after saving.
	/// </summary>
	[Theory]
	[InlineData(true, FormModes.Hidden)]
	[InlineData(false, FormModes.Edit)]
	public async Task SaveAsync_Create_CreatesTheItem(bool hideForm, FormModes expectedMode)
	{
		Person? created = null;
		var form = RenderForm(p => p
			.Add(x => x.HideForm, hideForm)
			.Add(x => x.Created, item => created = item));
		var field = AddField(form, p => p.Name);
		var person = new Person();
		await EditAsync(form, person, FormModes.Create);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Dee"));

		var saved = await form.InvokeAsync(() => form.Instance.SaveAsync());

		saved.Should().BeTrue();
		_provider.Created.Should().ContainSingle().Which.Name.Should().Be("Dee");
		created.Should().BeSameAs(person);
		form.Instance.Mode.Should().Be(expectedMode);
	}

	/// <summary>
	/// Verifies that a successful update sends the delta to the provider, applies it and raises Updated.
	/// </summary>
	[Fact]
	public async Task SaveAsync_Edit_UpdatesTheItem()
	{
		Person? updated = null;
		var form = RenderForm(p => p.Add(x => x.Updated, item => updated = item));
		var field = AddField(form, p => p.Name);
		var person = new Person { Name = "Ann" };
		await EditAsync(form, person, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));

		var saved = await form.InvokeAsync(() => form.Instance.SaveAsync());

		saved.Should().BeTrue();
		_provider.LastDelta.Should().ContainKey("Name").WhoseValue.Should().Be("Bob");
		person.Name.Should().Be("Bob");
		updated.Should().BeSameAs(person);
		form.Instance.Mode.Should().Be(FormModes.Hidden);
	}

	/// <summary>
	/// Verifies that a provider failure on create or update raises Error with its message and reports failure.
	/// </summary>
	[Theory]
	[InlineData(FormModes.Create)]
	[InlineData(FormModes.Edit)]
	public async Task SaveAsync_WhenTheProviderFails_RaisesError(FormModes mode)
	{
		string? error = null;
		_provider.Response = new OperationResponse { Success = false, ErrorMessage = "Refused" };
		var form = RenderForm(p => p.Add(x => x.Error, message => error = message));
		AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, mode);

		var saved = await form.InvokeAsync(() => form.Instance.SaveAsync());

		saved.Should().BeFalse();
		error.Should().Be("Refused");
		form.Instance.Mode.Should().Be(mode);
	}

	/// <summary>
	/// Verifies that an invalid item is not sent to the provider.
	/// </summary>
	[Fact]
	public async Task SaveAsync_WithValidationErrors_DoesNotSave()
	{
		var form = RenderForm();
		AddField(form, p => p.Name);
		await EditAsync(form, new Person(), FormModes.Create);

		var saved = await form.InvokeAsync(() => form.Instance.SaveAsync());

		saved.Should().BeFalse();
		_provider.Created.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a successful delete raises Deleted and hides the form, or returns it to create mode.
	/// </summary>
	[Theory]
	[InlineData(true, FormModes.Hidden)]
	[InlineData(false, FormModes.Create)]
	public async Task DeleteAsync_DeletesTheItem(bool hideForm, FormModes expectedMode)
	{
		Person? deleted = null;
		var form = RenderForm(p => p
			.Add(x => x.HideForm, hideForm)
			.Add(x => x.Deleted, item => deleted = item));
		var person = new Person { Name = "Ann" };
		await EditAsync(form, person, FormModes.Delete);

		var result = await form.InvokeAsync(() => form.Instance.DeleteAsync());

		result.Should().BeTrue();
		_provider.Deleted.Should().ContainSingle().Which.Should().BeSameAs(person);
		deleted.Should().BeSameAs(person);
		form.Instance.Mode.Should().Be(expectedMode);
	}

	/// <summary>
	/// Verifies that a provider failure on delete raises Error and reports failure.
	/// </summary>
	[Fact]
	public async Task DeleteAsync_WhenTheProviderFails_RaisesError()
	{
		string? error = null;
		_provider.Response = new OperationResponse { Success = false, ErrorMessage = "In use" };
		var form = RenderForm(p => p.Add(x => x.Error, message => error = message));
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Delete);

		var result = await form.InvokeAsync(() => form.Instance.DeleteAsync());

		result.Should().BeFalse();
		error.Should().Be("In use");
		form.Instance.Mode.Should().Be(FormModes.Delete);
	}
}
