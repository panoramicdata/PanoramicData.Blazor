using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFormFieldEditor{TItem}"/> chooses the right editor for a field's type and
/// options, writes edits back to its form, and is read-only in the modes that require it.
/// </summary>
public class PDFormFieldEditorTests : BunitContext
{
	private readonly Person _person = new();

	/// <summary>Sets up the rendering context.</summary>
	public PDFormFieldEditorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDForm<Person>> RenderForm(FormModes mode = FormModes.Edit)
		=> Render<PDForm<Person>>(parameters => parameters
			.Add(p => p.Item, _person)
			.Add(p => p.DefaultMode, mode));

	private IRenderedComponent<PDFormFieldEditor<Person>> RenderEditor(FormField<Person> field, FormModes mode = FormModes.Edit)
		=> RenderEditor(field, RenderForm(mode).Instance);

	private IRenderedComponent<PDFormFieldEditor<Person>> RenderEditor(FormField<Person> field, PDForm<Person> form)
		=> Render<PDFormFieldEditor<Person>>(parameters => parameters
			.Add(p => p.Form, form)
			.Add(p => p.Field, field));

	private static FormField<Person> FieldFor(System.Linq.Expressions.Expression<Func<Person, object>> expression)
		=> new() { Field = expression };

	/// <summary>A plain string field is edited in a text box that writes to the form's delta.</summary>
	[Fact]
	public void StringField_UsesATextBox_ThatWritesTheDelta()
	{
		var field = FieldFor(p => p.Name);
		field.Label = "Full name";
		var editor = RenderEditor(field);

		var input = editor.Find("input.form-control");
		input.GetAttribute("value").Should().Be("Ada");
		input.GetAttribute("placeholder").Should().Be("Full name");
		input.Change("Grace");

		editor.Instance.Form.Delta["Name"].Should().Be("Grace");
	}

	/// <summary>A Guid field is edited in a text box too.</summary>
	[Fact]
	public void GuidField_UsesATextBox()
	{
		var editor = RenderEditor(FieldFor(p => p.Reference));

		editor.Find("input.form-control").GetAttribute("value").Should().Be(_person.Reference.ToString());
	}

	/// <summary>A password field renders a password input that writes the delta, honouring the maximum length.</summary>
	[Fact]
	public void PasswordField_RendersAPasswordInput()
	{
		var field = FieldFor(p => p.Name);
		field.IsPassword = true;
		field.MaxLength = 20;
		field.AutoComplete = "new-password";
		var editor = RenderEditor(field);

		var input = editor.Find("input[type=password]");
		input.GetAttribute("maxlength").Should().Be("20");
		input.GetAttribute("autocomplete").Should().Be("new-password");
		input.Change("secret");

		editor.Instance.Form.Delta["Name"].Should().Be("secret");
	}

	/// <summary>A sensitive field is masked like a password.</summary>
	[Fact]
	public void SensitiveField_IsMasked()
	{
		var field = FieldFor(p => p.Name);
		field.IsSensitive = (_, _) => true;

		var editor = RenderEditor(field);

		editor.FindAll("input[type=password]").Should().ContainSingle();
	}

	/// <summary>An edit template replaces the built-in editor and is given the form's item.</summary>
	[Fact]
	public void EditTemplate_ReplacesTheEditor()
	{
		var field = FieldFor(p => p.Name);
		field.EditTemplate = item => b => b.AddMarkupContent(0, $"<em class=\"tpl\">{item?.Name}</em>");

		var editor = RenderEditor(field);

		editor.Find("em.tpl").TextContent.Should().Be("Ada");
		editor.FindAll("input").Should().BeEmpty();
	}

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

	/// <summary>A text area field uses a text area with the configured rows.</summary>
	[Fact]
	public void TextAreaField_UsesATextArea()
	{
		var field = FieldFor(p => p.Name);
		field.IsTextArea = true;
		field.TextAreaRows = 7;
		var editor = RenderEditor(field);

		var textarea = editor.Find("textarea");
		textarea.GetAttribute("rows").Should().Be("7");
		textarea.Input("Lovelace");

		editor.Instance.Form.Delta["Name"].Should().Be("Lovelace");
	}

	/// <summary>A string field with the text area editor option uses a non-resizing full height text area.</summary>
	[Fact]
	public void StringField_WithTextAreaOption_UsesAFullHeightTextArea()
	{
		var field = FieldFor(p => p.Name);
		field.DisplayOptions = new FieldStringOptions { Editor = FieldStringOptions.Editors.TextArea, Rows = 3, Resize = true, ResizeCssCls = "tall" };
		var editor = RenderEditor(field);

		var textarea = editor.Find("textarea");
		textarea.ClassList.Should().Contain(["h-100-pct", "no-resize"]);
		textarea.GetAttribute("rows").Should().Be("3");
		editor.Find("div.editor").ClassList.Should().Contain(["resize-h", "tall"]);
		textarea.Input("Typed");
		editor.Instance.Form.Delta["Name"].Should().Be("Typed");
	}

	/// <summary>A string field with the Monaco option renders a code editor in both editable and read-only modes.</summary>
	[Theory]
	[InlineData(FormModes.Edit)]
	[InlineData(FormModes.ReadOnly)]
	public void StringField_WithMonacoOption_RendersACodeEditor(FormModes mode)
	{
		var field = FieldFor(p => p.Name);
		field.DisplayOptions = new FieldStringOptions { Editor = FieldStringOptions.Editors.Monaco, CssClass = "code" };

		var editor = Render<PDFormFieldEditor<Person>>(parameters => parameters
			.Add(p => p.Form, RenderForm(mode).Instance)
			.Add(p => p.Field, field)
			.Add(p => p.Id, "monaco-1"));

		editor.Find("#monaco-1").ClassList.Should().Contain(["w-100", "code"]);
		editor.FindAll("input, textarea").Should().BeEmpty();
	}

	/// <summary>An image field renders the value as an image source.</summary>
	[Fact]
	public void ImageField_RendersAnImage()
	{
		var field = FieldFor(p => p.Name);
		field.IsImage = true;

		var editor = RenderEditor(field);

		editor.Find("img").GetAttribute("src").Should().Be("Ada");
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

	/// <summary>The editor is read-only in read-only, delete and cancel modes, with a readonly class in two of them.</summary>
	[Theory]
	[InlineData(FormModes.ReadOnly, true)]
	[InlineData(FormModes.Cancel, true)]
	[InlineData(FormModes.Delete, false)]
	public void ReadOnlyModes_DisableTheEditor(FormModes mode, bool expectReadOnlyClass)
	{
		var field = FieldFor(p => p.Age);

		var editor = RenderEditor(field, mode);

		editor.Instance.IsReadOnly(field).Should().BeTrue();
		editor.Find("input[type=number]").HasAttribute("disabled").Should().BeTrue();
		editor.Find("div.editor").ClassList.Contains("readonly").Should().Be(expectReadOnlyClass);
	}

	/// <summary>ReadOnlyInCreate and ReadOnlyInEdit apply only in their own mode.</summary>
	[Fact]
	public void ReadOnlyInCreateAndEdit_ApplyOnlyInTheirMode()
	{
		var createOnly = FieldFor(p => p.Age);
		createOnly.ReadOnlyInCreate = _ => true;
		var editOnly = FieldFor(p => p.Age);
		editOnly.ReadOnlyInEdit = _ => true;

		RenderEditor(createOnly, FormModes.Create).Instance.IsReadOnly(createOnly).Should().BeTrue();
		RenderEditor(createOnly, FormModes.Edit).Instance.IsReadOnly(createOnly).Should().BeFalse();
		RenderEditor(editOnly, FormModes.Edit).Instance.IsReadOnly(editOnly).Should().BeTrue();
		RenderEditor(editOnly, FormModes.Create).Instance.IsReadOnly(editOnly).Should().BeFalse();
	}

	/// <summary>A field with errors on the form is given the invalid class, alongside its own CSS class.</summary>
	[Fact]
	public void GetEditorClass_ReflectsErrorsAndTheFieldsClass()
	{
		var field = FieldFor(p => p.Age);
		field.DisplayOptions = new FieldDisplayOptions { CssClass = "wide" };
		var form = RenderForm().Instance;
		var editor = RenderEditor(field, form);

		editor.Instance.GetEditorClass(field).Trim().Should().Be("wide");

		form.SetFieldErrors("Age", "Too old");
		editor.Render();

		editor.Instance.GetEditorClass(field).Should().Contain("invalid").And.Contain("wide");
		editor.Find("input[type=number]").ClassList.Should().Contain("invalid");
	}

	/// <summary>ResetEditorCssAsync asks JS to clear the editor's inline style.</summary>
	[Fact]
	public async Task ResetEditorCssAsync_ClearsTheInlineStyle()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.Mode = JSRuntimeMode.Loose;
		var editor = RenderEditor(FieldFor(p => p.Age));

		await editor.InvokeAsync(editor.Instance.ResetEditorCssAsync);

		module.Invocations["clearInlineStyle"].Should().ContainSingle();
	}

	/// <summary>Leaving an editor stops suppressing the field's errors.</summary>
	[Fact]
	public void Blur_StopsSuppressingErrors()
	{
		var field = FieldFor(p => p.Age);
		field.SuppressErrors = true;
		var editor = RenderEditor(field);

		editor.Find("input[type=number]").Blur();

		field.SuppressErrors.Should().BeFalse();
	}

	/// <summary>Disposing the editor may be repeated safely.</summary>
	[Fact]
	public void Dispose_IsRepeatable()
	{
		var editor = RenderEditor(FieldFor(p => p.Age));

		editor.Instance.Dispose();
		var act = editor.Instance.Dispose;

		act.Should().NotThrow();
	}

	/// <summary>A colour with a display name on one member.</summary>
	public enum Colour
	{
		/// <summary>Red.</summary>
		Red,

		/// <summary>Sea green, which has a display name.</summary>
		[Display(Name = "Sea green")]
		SeaGreen,

		/// <summary>Blue.</summary>
		Blue
	}

	/// <summary>The item edited by the form under test.</summary>
	public sealed class Person
	{
		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = "Ada";

		/// <summary>Gets or sets an optional nickname.</summary>
		public string? Nickname { get; set; }

		/// <summary>Gets or sets the age.</summary>
		public int Age { get; set; } = 36;

		/// <summary>Gets or sets an optional score.</summary>
		public int? Score { get; set; }

		/// <summary>Gets or sets whether the person is active.</summary>
		public bool Active { get; set; } = true;

		/// <summary>Gets or sets an optional verification flag.</summary>
		public bool? Verified { get; set; }

		/// <summary>Gets or sets the birth date.</summary>
		public DateTime Born { get; set; } = new(1815, 12, 10, 10, 30, 0);

		/// <summary>Gets or sets an optional due date.</summary>
		public DateTime? Due { get; set; }

		/// <summary>Gets or sets when the person was last seen.</summary>
		public DateTimeOffset Seen { get; set; } = new(2020, 6, 1, 9, 0, 0, TimeSpan.FromHours(1));

		/// <summary>Gets or sets the favourite colour.</summary>
		public Colour Favourite { get; set; } = Colour.SeaGreen;

		/// <summary>Gets or sets a reference.</summary>
		public Guid Reference { get; set; } = Guid.NewGuid();

		/// <summary>Gets or sets an optional token.</summary>
		public Guid? Token { get; set; }
	}
}
