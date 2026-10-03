using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFormFieldEditor{TItem}"/> chooses the right editor for a field's type and
/// options, writes edits back to its form, and is read-only in the modes that require it.
/// </summary>
public partial class PDFormFieldEditorTests : BunitContext
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
		RenderEditor(createOnly, FormModes.Empty).Instance.IsReadOnly(createOnly).Should().BeFalse();
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

		/// <summary>A colour held in a field rather than a property.</summary>
		internal Colour FavouriteField = Colour.Red;

		/// <summary>Gets or sets a reference.</summary>
		public Guid Reference { get; set; } = Guid.NewGuid();

		/// <summary>Gets or sets an optional token.</summary>
		public Guid? Token { get; set; }
	}
}
