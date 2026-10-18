using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests of how a <see cref="PDCardDeck{TCard}"/> starts up: its loading icon, and the order in which it loads its
/// cards, joins its group and loads its script.
/// </summary>
public partial class PDCardDeckTests
{
	/// <summary>
	/// Verifies that a deck outside a group told not to show a loading icon shows none while its cards load.
	/// </summary>
	[Fact]
	public void Standalone_ShowLoadingIconOff_ShowsNoIconWhileLoading()
	{
		var pending = new TaskCompletionSource<DataResponse<Card>>();
		var deck = Render<PDCardDeck<Card>>(parameters => parameters
			.Add(p => p.DataFunction, () => pending.Task)
			.Add(p => p.ShowLoadingIcon, false));

		deck.Instance.DataLoaded.Should().BeFalse();
		deck.FindComponents<PDCardDeckLoadingIcon>().Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a deck outside a group gives its loading icon the deck's message and elapsed-time choice.
	/// </summary>
	[Fact]
	public void Standalone_PassesTheLoadingMessageAndElapsedTimeToTheIcon()
	{
		var pending = new TaskCompletionSource<DataResponse<Card>>();
		var deck = Render<PDCardDeck<Card>>(parameters => parameters
			.Add(p => p.DataFunction, () => pending.Task)
			.Add(p => p.LoadingMessage, "Fetching the tickets")
			.Add(p => p.ShowLoadingElapsedTime, false));

		var icon = deck.FindComponent<PDCardDeckLoadingIcon>().Instance;
		icon.Message.Should().Be("Fetching the tickets");
		icon.ShowElapsedTime.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that decks in a group show their cards and join the group before their script is ready, so a full page
	/// load shows no loading icon while the script downloads, and that the cards cannot be dragged until the script's
	/// drop listener is in place.
	/// </summary>
	[Fact]
	public void Group_DecksShowTheirCards_BeforeTheirScriptIsReady()
	{
		// Never answered, so each deck's script start-up stops at its listeners.
		JSInterop.SetupModule(ModulePath).SetupVoid("registerValidDragOperationListeners", _ => true);
		var board = new Board();

		var group = Render<PDCardDeckGroup<Card>>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<Card>())
			.AddChildContent<PDCardDeck<Card>>(deck => deck
				.Add(p => p.Id, "left")
				.Add(p => p.DataFunction, board.DataFor("left")))
			.AddChildContent<PDCardDeck<Card>>(deck => deck
				.Add(p => p.Id, "right")
				.Add(p => p.DataFunction, board.DataFor("right"))));

		group.WaitForAssertion(() => group.FindAll("div.card").Should().HaveCount(3));
		group.Instance.AllDataLoaded().Should().BeTrue();
		group.FindComponents<PDCardDeckLoadingIcon>().Should().BeEmpty();
		group.FindAll("div.card").Should().AllSatisfy(c => c.GetAttribute("draggable").Should().Be("false"));
	}

	/// <summary>
	/// Verifies that a deck disposed while its cards are loading neither joins its group nor loads its script, so it
	/// leaves nothing behind that would outlive it (#216).
	/// </summary>
	[Fact]
	public async Task DisposedWhileLoading_NeitherJoinsTheGroupNorLoadsTheScript()
	{
		var pending = new TaskCompletionSource<DataResponse<Card>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var group = Render<PDCardDeckGroup<Card>>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<Card>())
			.AddChildContent<PDCardDeck<Card>>(deck => deck.Add(p => p.DataFunction, () => pending.Task)));
		var groupInstance = group.Instance;

		await DisposeComponentsAsync();
		pending.SetResult(new DataResponse<Card>(Cards(), 3));
		// The deck's start-up resumes on the renderer, so this runs only once it has finished.
		await Renderer.Dispatcher.InvokeAsync(() => { });

		groupInstance.Decks.Should().BeEmpty();
		JSInterop.VerifyNotInvoke("import");
	}
}
