using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// How <see cref="PDContextMenu"/> contains failures: given an ExceptionHandler, a failure in showing the menu is passed
/// to it rather than ending the Blazor circuit, and without one it is thrown as before.
/// </summary>
public partial class PDContextMenuTests
{
	private static Task RightClickAsync(IRenderedComponent<PDContextMenu> component)
		=> component.InvokeAsync(() => component.Find(".pdcontextmenuhost").MouseDownAsync(new MouseEventArgs { Button = 2 }));

	/// <summary>A failure in showing the menu, such as its script failing, is passed to ExceptionHandler.</summary>
	[Fact]
	public async Task AFailureShowingTheMenu_IsPassedToTheExceptionHandler()
	{
		_module.SetupVoid("showMenu", _ => true).SetException(new JSException("No menu"));
		var failures = new List<Exception>();
		var component = Render<PDContextMenu>(parameters => parameters
			.Add(p => p.Items, CreateItems())
			.Add(p => p.ExceptionHandler, failures.Add)
			.AddChildContent("<span class=\"target\">Right click me</span>"));

		await RightClickAsync(component);

		failures.Should().ContainSingle().Which.Message.Should().Be("No menu");
	}

	/// <summary>Without an ExceptionHandler the failure is thrown, as it always was.</summary>
	[Fact]
	public async Task WithoutAnExceptionHandler_AFailureShowingTheMenu_IsThrownAsBefore()
	{
		_module.SetupVoid("showMenu", _ => true).SetException(new JSException("No menu"));
		var component = RenderMenu(null);

		var act = () => RightClickAsync(component);

		await act.Should().ThrowAsync<JSException>().WithMessage("No menu");
	}
}
