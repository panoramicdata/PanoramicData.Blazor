using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDForm{TItem}"/> tracks edits as a delta, validates them, and saves, updates and
/// deletes through its data provider.
/// </summary>
public class PDFormTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDForm.razor.js";
	private readonly PersonProvider _provider = new();
	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context.</summary>
	public PDFormTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		_module = JSInterop.SetupModule(ModulePath);
	}

	private IRenderedComponent<PDForm<Person>> RenderForm(Action<ComponentParameterCollectionBuilder<PDForm<Person>>>? configure = null)
		=> Render<PDForm<Person>>(parameters =>
		{
			parameters.Add(p => p.DataProvider, _provider);
			configure?.Invoke(parameters);
		});

	private static FormField<Person> AddField(IRenderedComponent<PDForm<Person>> form, Expression<Func<Person, object>> field, Action<FormField<Person>>? configure = null)
	{
		var definition = new FormField<Person> { Field = field };
		configure?.Invoke(definition);
		form.Instance.Fields.Add(definition);
		return definition;
	}

	private static async Task<IRenderedComponent<PDForm<Person>>> EditAsync(IRenderedComponent<PDForm<Person>> form, Person? item, FormModes mode, bool? validate = null)
	{
		await form.InvokeAsync(() => form.Instance.EditItemAsync(item, mode, true, validate));
		return form;
	}

	private int UnloadListenerCalls(bool armed)
		=> _module.Invocations["setUnloadListener"].Count(i => Equals(i.Arguments[1], armed));

	/// <summary>
	/// Verifies that the form wraps its content in a div carrying its CSS class, and starts in its default mode.
	/// </summary>
	[Fact]
	public void Render_WrapsContent_AndStartsInTheDefaultMode()
	{
		var form = RenderForm(p => p
			.Add(x => x.CssClass, "person-form")
			.Add(x => x.DefaultMode, FormModes.Edit)
			.Add(x => x.ChildContent, "<span class=\"inside\">content</span>"));

		form.Find("div.pd-form.person-form span.inside").TextContent.Should().Be("content");
		form.Instance.Mode.Should().Be(FormModes.Edit);
		form.Instance.Id.Should().StartWith("pd-form-");
		form.Instance.HasChanges.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that a <see cref="PDField{TItem}"/> declared inside the form registers itself, copying its
	/// definition and taking the title from the display attribute when none is given.
	/// </summary>
	[Fact]
	public void AddFieldAsync_FromADeclaredField_CopiesTheDefinition()
	{
		var form = RenderForm(p => p.Add(x => x.ChildContent, builder =>
		{
			builder.OpenComponent<PDFormBody<Person>>(0);
			builder.AddAttribute(1, nameof(PDFormBody<Person>.ChildContent), (RenderFragment)(body =>
			{
				body.OpenComponent<PDField<Person>>(0);
				body.AddAttribute(1, nameof(PDField<Person>.Field), (Expression<Func<Person, object>>)(p => p.Name));
				body.AddAttribute(2, nameof(PDField<Person>.MaxLength), (int?)20);
				body.AddAttribute(3, nameof(PDField<Person>.Group), "Main");
				body.AddAttribute(4, nameof(PDField<Person>.IsTextArea), true);
				body.AddAttribute(5, nameof(PDField<Person>.HelpUrl), "https://help");
				body.CloseComponent();
			}));
			builder.CloseComponent();
		}));

		var field = form.Instance.Fields.Should().ContainSingle().Subject;
		field.Title.Should().Be("Full name");
		field.MaxLength.Should().Be(20);
		field.Group.Should().Be("Main");
		field.IsTextArea.Should().BeTrue();
		field.HelpUrl.Should().Be("https://help");
		form.Instance.GetField("Name").Should().BeSameAs(field);
	}

	/// <summary>
	/// Verifies that entering create mode validates the item, records the previous mode and suppresses the
	/// errors for display until the first edit.
	/// </summary>
	[Fact]
	public async Task EditItemAsync_Create_ValidatesAndSuppressesInitialErrors()
	{
		var form = RenderForm(p => p.Add(x => x.DefaultMode, FormModes.Edit));
		var field = AddField(form, p => p.Name);

		await EditAsync(form, new Person(), FormModes.Create);

		form.Instance.Mode.Should().Be(FormModes.Create);
		form.Instance.PreviousMode.Should().Be(FormModes.Edit);
		form.Instance.Errors.Should().ContainKey("Name");
		form.Instance.IsValid().Should().BeFalse();
		field.SuppressErrors.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that validation can be turned off, and that the delete mode does not validate by default.
	/// </summary>
	[Theory]
	[InlineData(FormModes.Create, false)]
	[InlineData(FormModes.Delete, null)]
	public async Task EditItemAsync_WithoutValidation_RecordsNoErrors(FormModes mode, bool? validate)
	{
		var form = RenderForm();
		AddField(form, p => p.Name);

		await EditAsync(form, new Person(), mode, validate);

		form.Instance.Errors.Should().BeEmpty();
		form.Instance.Item.Should().NotBeNull();
	}

	/// <summary>
	/// Verifies that an edit is recorded as a delta, announced with its old and new values, pushed to the
	/// field, and arms the unload guard.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_RecordsTheDelta_AndAnnouncesIt()
	{
		var updates = new List<FieldUpdateArgs<Person>>();
		var form = RenderForm(p => p.Add(x => x.FieldUpdated, args => updates.Add(args)));
		var field = AddField(form, p => p.Name);
		var person = new Person { Name = "Ann" };
		await EditAsync(form, person, FormModes.Edit);
		object? notified = null;
		field.ValueChanged += (_, value) => notified = value;

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Cy"));

		form.Instance.Delta.Should().ContainKey("Name").WhoseValue.Should().Be("Cy");
		form.Instance.HasChanges.Should().BeTrue();
		person.Name.Should().Be("Ann");
		updates.Select(u => (u.OldValue, u.NewValue)).Should().Equal(("Ann", "Bob"), ("Bob", "Cy"));
		notified.Should().Be("Cy");
		_module.Invocations["setUnloadListener"][^1].Arguments.Should().Equal(form.Instance.Id, true);
	}

	/// <summary>
	/// Verifies that setting a field back to its original value removes it from the delta and disarms the
	/// unload guard.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_RevertingToTheOriginal_RemovesTheDelta()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		var disarmedBefore = UnloadListenerCalls(false);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Ann"));

		form.Instance.Delta.Should().BeEmpty();
		form.Instance.HasChanges.Should().BeFalse();
		UnloadListenerCalls(false).Should().Be(disarmedBefore + 1);
	}

	/// <summary>
	/// Verifies that an unchanged value, including one that differs only in line endings, is not an edit.
	/// </summary>
	[Theory]
	[InlineData("first\r\nsecond", "first\nsecond")]
	[InlineData("same", "same")]
	public async Task SetFieldValueAsync_WithAnEquivalentValue_IsNotAnEdit(string original, string entered)
	{
		var updates = 0;
		var form = RenderForm(p => p.Add(x => x.FieldUpdated, _ => updates++));
		var field = AddField(form, p => p.Notes);
		await EditAsync(form, new Person { Notes = original }, FormModes.Edit);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, entered));

		form.Instance.Delta.Should().BeEmpty();
		updates.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a value of another type is converted to the property type before it is recorded.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_ConvertsTheValueToThePropertyType()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Age);
		await EditAsync(form, new Person { Name = "Ann", Age = 30 }, FormModes.Edit);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, 42L));

		form.Instance.Delta["Age"].Should().Be(42);
		form.Instance.GetFieldValue<int>("Age").Should().Be(42);
		form.Instance.GetFieldValue<int>("Age", false).Should().Be(30);
	}

	/// <summary>
	/// Verifies that with automatic delta application an edit in edit mode is written straight to the item,
	/// and that suppressing field notification does not tell the field.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_WithAutoApplyDelta_UpdatesTheItem()
	{
		var form = RenderForm(p => p.Add(x => x.AutoApplyDelta, true));
		var field = AddField(form, p => p.Name);
		var person = new Person { Name = "Ann" };
		await EditAsync(form, person, FormModes.Edit);
		var notified = false;
		field.ValueChanged += (_, _) => notified = true;

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob", notifyField: false));

		person.Name.Should().Be("Bob");
		notified.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that with no item, or a field with no binding, a value is ignored.
	/// </summary>
	[Fact]
	public async Task SetFieldValueAsync_WithoutAnItemOrBinding_IsIgnored()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		var unbound = new FormField<Person>();

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));
		await EditAsync(form, new Person(), FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(unbound, "Bob"));

		form.Instance.Delta.Should().BeEmpty();
	}

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

	/// <summary>
	/// Verifies that field values are read from the delta when asked for the updated value, and from the item
	/// when asked for the original.
	/// </summary>
	[Fact]
	public async Task GetFieldValue_ReadsTheUpdatedOrOriginalValue()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));

		form.Instance.GetFieldValue("Name").Should().Be("Bob");
		form.Instance.GetFieldValue("Name", false).Should().Be("Ann");
		form.Instance.GetFieldValue(field).Should().Be("Bob");
		form.Instance.GetFieldStringValue("Name").Should().Be("Bob");
		form.Instance.GetFieldStringValue("Name", false).Should().Be("Ann");
		form.Instance.GetFieldStringValue(field).Should().Be("Bob");
	}

	/// <summary>
	/// Verifies that a field bound to a computed expression rather than a property is evaluated against the item.
	/// </summary>
	[Fact]
	public async Task GetFieldValue_OfAComputedField_EvaluatesTheExpression()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Age * 2);
		await EditAsync(form, new Person { Name = "Ann", Age = 21 }, FormModes.Edit, validate: false);

		form.Instance.GetFieldValue(field).Should().Be(42);
		form.Instance.GetFieldValue<int>(field).Should().Be(42);
		form.Instance.GetFieldStringValue(field).Should().Be("42");
	}

	/// <summary>
	/// Verifies that typed reads convert compatible values and fall back to the default for incompatible or
	/// missing ones.
	/// </summary>
	[Fact]
	public async Task GetFieldValueOfT_ConvertsOrFallsBackToDefault()
	{
		var form = RenderForm();
		var name = AddField(form, p => p.Name);
		var age = AddField(form, p => p.Age);

		form.Instance.GetFieldValue<int>(age).Should().Be(0, "there is no item yet");
		await EditAsync(form, new Person { Name = "12", Age = 7 }, FormModes.Edit);

		form.Instance.GetFieldValue<int>(age).Should().Be(7);
		form.Instance.GetFieldValue<long>("Age").Should().Be(7L);
		form.Instance.GetFieldValue<int>(name).Should().Be(12);
		form.Instance.Item!.Name = "twelve";
		form.Instance.GetFieldValue<int>(name).Should().Be(0);
		form.Instance.GetFieldValue<int>((FormField<Person>)null!, true).Should().Be(0);
	}

	/// <summary>
	/// Verifies that string reads format dates as ISO dates, render null as empty and join several fields with tabs.
	/// </summary>
	[Fact]
	public async Task GetFieldStringValue_FormatsDates_AndJoinsFields()
	{
		var form = RenderForm();
		var born = AddField(form, p => p.Born);
		var joined = AddField(form, p => p.Joined!);
		var nickname = AddField(form, p => p.Nickname!);
		var person = new Person
		{
			Name = "Ann",
			Born = new DateTime(1990, 5, 17, 13, 45, 0, DateTimeKind.Unspecified),
			Joined = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero)
		};

		form.Instance.GetFieldStringValue(born).Should().BeEmpty("there is no item yet");
		form.Instance.GetFieldValue(born).Should().BeNull();
		await EditAsync(form, person, FormModes.Edit, validate: false);

		form.Instance.GetFieldStringValue(born).Should().Be("1990-05-17");
		form.Instance.GetFieldStringValue(joined).Should().Be("2020-01-02");
		form.Instance.GetFieldStringValue(nickname).Should().BeEmpty();
		form.Instance.GetFieldStringValue([born, nickname, joined]).Should().Be("1990-05-17\t\t2020-01-02");
		form.Instance.GetFieldStringValue([born], false).Should().Be("1990-05-17");
	}

	/// <summary>
	/// Verifies that a clone with the edits applied is returned without changing the item, and none without an item.
	/// </summary>
	[Fact]
	public async Task GetItemWithUpdates_ReturnsAnUpdatedClone()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		form.Instance.GetItemWithUpdates().Should().BeNull();
		var person = new Person { Name = "Ann", Age = 3 };
		await EditAsync(form, person, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));

		var clone = form.Instance.GetItemWithUpdates();

		clone.Should().NotBeNull().And.NotBeSameAs(person);
		clone!.Name.Should().Be("Bob");
		clone.Age.Should().Be(3);
		person.Name.Should().Be("Ann");
	}

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

	/// <summary>A person edited by the form.</summary>
	[Display(Name = "Person record")]
	public sealed class Person
	{
		/// <summary>Gets or sets the name.</summary>
		[Required]
		[Display(Name = "Full name")]
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets the age.</summary>
		public int Age { get; set; }

		/// <summary>Gets or sets free text notes.</summary>
		public string Notes { get; set; } = string.Empty;

		/// <summary>Gets or sets an optional nickname.</summary>
		public string? Nickname { get; set; }

		/// <summary>Gets or sets the date of birth.</summary>
		public DateTime Born { get; set; }

		/// <summary>Gets or sets when the person joined.</summary>
		public DateTimeOffset? Joined { get; set; }
	}

	/// <summary>A provider that records what the form asks of it and answers with a configurable response.</summary>
	private sealed class PersonProvider : DataProviderBase<Person>
	{
		public OperationResponse Response { get; set; } = new() { Success = true };

		public List<Person> Created { get; } = [];

		public List<Person> Deleted { get; } = [];

		public List<Person> Updated { get; } = [];

		public Dictionary<string, object?> LastDelta { get; private set; } = [];

		public override Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Created.Add(item);
			return Task.FromResult(Response);
		}

		public override Task<OperationResponse> UpdateAsync(Person item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Updated.Add(item);
			LastDelta = new Dictionary<string, object?>(delta);
			return Task.FromResult(Response);
		}

		public override Task<OperationResponse> DeleteAsync(Person item, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (Response.Success)
			{
				Deleted.Add(item);
			}

			return Task.FromResult(Response);
		}
	}
}
