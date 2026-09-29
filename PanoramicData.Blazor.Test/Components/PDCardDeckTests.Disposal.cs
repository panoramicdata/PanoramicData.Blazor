using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that disposing a <see cref="PDCardDeck{TCard}"/> removes the document-level listeners its script added (#216).
/// </summary>
public partial class PDCardDeckTests
{
	/// <summary>
	/// Verifies that disposing a deck asks its script to remove the listeners registered for the deck's element.
	/// </summary>
	[Fact]
	public async Task Disposing_UnregistersTheListenersForItsElement()
	{
		var module = JSInterop.SetupModule(ModulePath);
		RenderDeck();
		var element = module.VerifyInvoke("registerValidDragOperationListeners").Arguments[0]
			.Should().BeOfType<ElementReference>().Subject;

		await DisposeComponentsAsync();

		module.VerifyInvoke("unregisterListeners").Arguments[0].Should().Be(element);
	}

	/// <summary>
	/// Verifies that a deck disposed after its circuit has gone does not fail, since the browser has already
	/// dropped the listeners with the page.
	/// </summary>
	[Fact]
	public async Task Disposing_AfterTheCircuitHasGone_DoesNotThrow()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("unregisterListeners").SetException(new JSDisconnectedException("The circuit has gone."));
		RenderDeck();

		var dispose = () => DisposeComponentsAsync();

		await dispose.Should().NotThrowAsync();
		module.VerifyInvoke("unregisterListeners");
	}
}
