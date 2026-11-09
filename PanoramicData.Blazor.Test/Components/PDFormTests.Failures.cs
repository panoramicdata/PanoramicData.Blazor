using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// A failure inside <see cref="PDForm{TItem}"/> never escapes to Blazor, where it would end the user's circuit.
/// </summary>
public partial class PDFormTests
{
	/// <summary>A browser that cannot clear the editors' styles still lets the form open.</summary>
	[Fact]
	public async Task EditItemAsync_WhenClearingStylesFails_StillOpensTheForm()
	{
		var common = SetupCommonModule();
		common.SetupVoid("clearInlineStyles", _ => true).SetException(new JSException("clearInlineStyles is not a function"));
		common.SetupVoid("clearInlineStyle", _ => true).SetException(new JSException("clearInlineStyle is not a function"));
		var form = RenderForm();
		RenderEditor(form, AddField(form, p => p.Name));

		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		form.Instance.Mode.Should().Be(FormModes.Edit);
		Renderer.UnhandledException.IsCompleted.Should().BeFalse();
	}

	/// <summary>A browser that cannot arm the unsaved changes prompt still lets the user edit.</summary>
	[Fact]
	public async Task SetFieldValueAsync_WhenTheUnloadGuardFails_StillRecordsTheChange()
	{
		_module.SetupVoid("setUnloadListener", _ => true).SetException(new JSException("The page has gone"));
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));

		form.Instance.Delta.Should().ContainKey(nameof(Person.Name));
	}

	/// <summary>With no exception handler, the message reaches the host through Error.</summary>
	[Fact]
	public async Task HandleExceptionAsync_WithNoExceptionHandler_RaisesError()
	{
		var errors = new List<string>();
		var form = RenderForm(p => p.Add(x => x.Error, (string message) => errors.Add(message)));

		await form.InvokeAsync(() => form.Instance.HandleExceptionAsync(new InvalidOperationException("The server is down")));

		errors.Should().Equal("The server is down");
	}

	/// <summary>With an exception handler, the exception goes there and Error is not raised as well.</summary>
	[Fact]
	public async Task HandleExceptionAsync_WithAnExceptionHandler_RaisesOnlyThat()
	{
		var errors = new List<string>();
		var handled = new List<Exception>();
		var form = RenderForm(p => p
			.Add(x => x.Error, (string message) => errors.Add(message))
			.Add(x => x.ExceptionHandler, (Exception ex) => handled.Add(ex)));
		var failure = new InvalidOperationException("The server is down");

		await form.InvokeAsync(() => form.Instance.HandleExceptionAsync(failure));

		handled.Should().Equal(failure);
		errors.Should().BeEmpty();
	}

	/// <summary>An exception handler that throws does not make the form throw.</summary>
	[Fact]
	public async Task HandleExceptionAsync_WhenTheExceptionHandlerThrows_DoesNotThrow()
	{
		var form = RenderForm(p => p.Add(x => x.ExceptionHandler, (Exception _) => throw new InvalidOperationException("The handler failed")));

		var act = () => form.InvokeAsync(() => form.Instance.HandleExceptionAsync(new InvalidOperationException("The server is down")));

		await act.Should().NotThrowAsync();
	}
}
