using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Validation, reset, unload guard and lifecycle tests for <see cref="PDForm{TItem}"/>.
/// </summary>
public partial class PDFormTests
{
	/// <summary>
	/// Verifies that numeric limits on a field produce errors for values outside them, and none inside.
	/// </summary>
	[Theory]
	[InlineData(200, "Value must be 150 or less.")]
	[InlineData(-1, "Value must be 0 or greater.")]
	[InlineData(40, null)]
	public async Task ValidateFieldAsync_EnforcesNumericLimits(int age, string? expectedError)
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Age, f =>
		{
			f.MaxValue = 150;
			f.MinValue = 0;
		});
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		var typed = await form.InvokeAsync(() => form.Instance.ValidateFieldAsync(field, age));

		typed.Should().Be(age);
		if (expectedError is null)
		{
			form.Instance.Errors.Should().NotContainKey("Age");
		}
		else
		{
			form.Instance.Errors["Age"].Should().Equal(expectedError);
		}
	}

	/// <summary>
	/// Verifies that a value that cannot be converted to the property type is reported as an error on the field.
	/// </summary>
	[Fact]
	public async Task ValidateFieldAsync_WithAnUnconvertibleValue_RecordsAnError()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Age);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		var typed = await form.InvokeAsync(() => form.Instance.ValidateFieldAsync(field, "not a number"));

		typed.Should().BeNull();
		form.Instance.Errors.Should().ContainKey("Age").WhoseValue.Should().NotBeEmpty();
	}

	/// <summary>
	/// Verifies that custom validation can add errors to any field and remove errors raised by the standard rules.
	/// </summary>
	[Fact]
	public async Task CustomValidate_AddsAndRemovesErrors()
	{
		var form = RenderForm(p => p.Add(x => x.CustomValidate, args =>
		{
			if (args.Field.Name == "Name")
			{
				args.RemoveErrorMessages.Add("Name", "The Full name field is required.");
				args.AddErrorMessages.Add("Notes", "Notes are needed when there is no name.");
			}
		}));
		AddField(form, p => p.Name);

		await EditAsync(form, new Person(), FormModes.Create);

		form.Instance.Errors.Should().NotContainKey("Name");
		form.Instance.Errors["Notes"].Should().Equal("Notes are needed when there is no name.");
	}

	/// <summary>
	/// Verifies that removing errors through custom validation announces the change even when none are added.
	/// </summary>
	[Fact]
	public async Task CustomValidate_RemovingOnly_RaisesErrorsChanged()
	{
		var form = RenderForm(p => p.Add(x => x.CustomValidate, args =>
			args.RemoveErrorMessages.Add("Name", "The Full name field is required.")));
		AddField(form, p => p.Name);
		var changes = 0;
		form.Instance.ErrorsChanged += (_, _) => changes++;

		await EditAsync(form, new Person(), FormModes.Create);

		form.Instance.IsValid().Should().BeTrue();
		changes.Should().BeGreaterThan(0);
	}

	/// <summary>
	/// Verifies that field errors are de-duplicated, cleared per field, and announced each time.
	/// </summary>
	[Fact]
	public void SetFieldErrorsAndClearErrors_MaintainTheErrorList()
	{
		var form = RenderForm();
		var changes = 0;
		form.Instance.ErrorsChanged += (_, _) => changes++;

		form.Instance.SetFieldErrors("Name", "Too short", "Too short", "No digits");
		form.Instance.SetFieldErrors("Name", "No digits");
		form.Instance.Errors["Name"].Should().Equal("Too short", "No digits");
		form.Instance.IsValid().Should().BeFalse();

		form.Instance.ClearErrors("Name");

		form.Instance.IsValid().Should().BeTrue();
		changes.Should().Be(3);
	}

	/// <summary>
	/// Verifies that resetting discards edits and errors, asks listeners to reset, and disarms the unload guard.
	/// </summary>
	[Fact]
	public async Task ResetChanges_DiscardsEditsAndErrors()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, string.Empty));
		var resets = 0;
		form.Instance.ResetRequested += (_, _) => resets++;
		var disarmedBefore = UnloadListenerCalls(false);

		await form.InvokeAsync(() => form.Instance.ResetChanges());

		form.Instance.Delta.Should().BeEmpty();
		form.Instance.Errors.Should().BeEmpty();
		resets.Should().Be(1);
		UnloadListenerCalls(false).Should().Be(disarmedBefore + 1);
	}

	/// <summary>
	/// Verifies that the unload guard is left alone when the form is configured not to confirm on unload.
	/// </summary>
	[Fact]
	public async Task ConfirmOnUnloadOff_NeverTouchesTheUnloadGuard()
	{
		var form = RenderForm(p => p.Add(x => x.ConfirmOnUnload, false));
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Ann"));
		await form.InvokeAsync(() => form.Instance.DisposeAsync().AsTask());

		_module.Invocations["setUnloadListener"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a navigation away from a form with unsaved changes is held for confirmation, and one
	/// from a clean form is not.
	/// </summary>
	[Fact]
	public async Task Navigation_WithUnsavedChanges_AsksForConfirmation()
	{
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		common.Setup<bool>("confirm", _ => true).SetResult(false);
		var form = RenderForm();
		var navigation = Services.GetRequiredService<INavigationCancelService>();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		(await navigation.ProceedAsync("/elsewhere")).Should().BeTrue();
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));

		(await navigation.ProceedAsync("/elsewhere")).Should().BeFalse();
		common.VerifyInvoke("confirm");
	}

	/// <summary>
	/// Verifies that disposing a dirty form disarms the unload guard and stops it holding navigation.
	/// </summary>
	[Fact]
	public async Task DisposeAsync_DisarmsTheGuards()
	{
		var form = RenderForm();
		var navigation = Services.GetRequiredService<INavigationCancelService>();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		var disarmedBefore = UnloadListenerCalls(false);

		await form.InvokeAsync(() => form.Instance.DisposeAsync().AsTask());

		UnloadListenerCalls(false).Should().Be(disarmedBefore + 1);
		(await navigation.ProceedAsync("/elsewhere")).Should().BeTrue();
	}

	/// <summary>
	/// Verifies that exceptions handed to the form are passed to the exception handler.
	/// </summary>
	[Fact]
	public async Task HandleExceptionAsync_RaisesTheExceptionHandler()
	{
		Exception? handled = null;
		var form = RenderForm(p => p.Add(x => x.ExceptionHandler, ex => handled = ex));
		var exception = new InvalidOperationException("boom");

		await form.InvokeAsync(() => form.Instance.HandleExceptionAsync(exception));

		handled.Should().BeSameAs(exception);
	}

	/// <summary>
	/// Verifies that changing whether help is shown re-renders the form only when the value changes.
	/// </summary>
	[Fact]
	public async Task ShowHelp_RerendersOnlyOnChange()
	{
		var form = RenderForm();
		var before = form.RenderCount;
		await form.InvokeAsync(() => form.Instance.ShowHelp = true);
		var afterChange = form.RenderCount;

		await form.InvokeAsync(() => form.Instance.ShowHelp = true);

		form.Instance.ShowHelp.Should().BeTrue();
		afterChange.Should().BeGreaterThan(before);
		form.RenderCount.Should().Be(afterChange);
	}

	/// <summary>
	/// Verifies that the deprecated item and mode setters still set the item and mode, reset edits and
	/// clear errors.
	/// </summary>
	[Fact]
	[Obsolete("Pins the behaviour of the deprecated SetItem and SetMode methods.")]
	public async Task DeprecatedSetItemAndSetMode_StillWork()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		var person = new Person { Name = "Ann" };
		form.Instance.SetItem(person);
		await form.InvokeAsync(() => form.Instance.SetMode(FormModes.Edit));
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, string.Empty));
		form.Instance.Errors.Should().NotBeEmpty();

		await form.InvokeAsync(() => form.Instance.SetMode(FormModes.Create));

		form.Instance.Item.Should().BeSameAs(person);
		form.Instance.Mode.Should().Be(FormModes.Create);
		form.Instance.PreviousMode.Should().Be(FormModes.Edit);
		form.Instance.Delta.Should().BeEmpty();
		form.Instance.Errors.Should().BeEmpty();
		field.SuppressErrors.Should().BeTrue();
	}
}
