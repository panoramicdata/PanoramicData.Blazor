using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// A failure behind a field editor goes to the form, never to Blazor, where it would end the user's circuit.
/// </summary>
public partial class PDFormFieldEditorTests
{
	/// <summary>A host FieldUpdated handler that throws when a value is chosen hands the exception to the form.</summary>
	[Fact]
	public void ChoosingAValue_WhenTheHostsFieldUpdatedThrows_GivesTheFormTheException()
	{
		var handled = new List<Exception>();
		var form = Render<PDForm<Person>>(parameters => parameters
			.Add(p => p.Item, _person)
			.Add(p => p.DefaultMode, FormModes.Edit)
			.Add(p => p.FieldUpdated, (FieldUpdateArgs<Person> _) => throw new InvalidOperationException("The host failed"))
			.Add(p => p.ExceptionHandler, (Exception ex) => handled.Add(ex)));
		var field = FieldFor(p => p.Name);
		field.Options = (_, _) => [new OptionInfo { Text = "Ada", Value = "Ada" }, new OptionInfo { Text = "Bea", Value = "Bea" }];
		var editor = RenderEditor(field, form.Instance);

		editor.Find("select").Input("Bea");

		handled.Should().ContainSingle().Which.Message.Should().Be("The host failed");
		Renderer.UnhandledException.IsCompleted.Should().BeFalse();
	}

	/// <summary>A Monaco editor that fails while the form resets its value does not end the circuit.</summary>
	[Fact]
	public async Task ResetChanges_WhenMonacoFails_DoesNotEndTheCircuit()
	{
		var field = FieldFor(p => p.Name);
		field.DisplayOptions = new FieldStringOptions { Editor = FieldStringOptions.Editors.Monaco };
		var form = RenderForm(FormModes.Edit);
		RenderEditor(field, form.Instance);
		JSInterop.SetupVoid(_ => true).SetException(new JSException("The editor has gone"));

		await form.InvokeAsync(form.Instance.ResetChanges);

		Renderer.UnhandledException.IsCompleted.Should().BeFalse();
	}
}
