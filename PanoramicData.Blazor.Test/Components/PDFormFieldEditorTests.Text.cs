using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Text, password, code and image editor tests for <see cref="PDFormFieldEditor{TItem}"/>.
/// </summary>
public partial class PDFormFieldEditorTests
{
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
}
