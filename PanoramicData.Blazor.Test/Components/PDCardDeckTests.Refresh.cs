using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests of refreshing <see cref="PDCardDeck{TCard}"/> decks, alone and in a <see cref="PDCardDeckGroup{TCard}"/>, after
/// something outside them changes which cards they hold (#263).
/// </summary>
public partial class PDCardDeckTests
{
	private static Card CopyOf(Card card) => new(card.Name, card.DeckPosition) { Id = card.Id };

	/// <summary>
	/// Verifies that a refresh shows the cards the data function returns now.
	/// </summary>
	[Fact]
	public async Task RefreshAsync_ShowsTheCardsTheDataFunctionNowReturns()
	{
		var cards = Cards();
		var deck = RenderDeck(cards);
		cards.RemoveAll(card => card.Name == "Bravo");
		cards.Add(new Card("Delta", 3));

		await deck.InvokeAsync(deck.Instance.RefreshAsync);

		Names(deck).Should().Equal("Alpha", "Charlie", "Delta");
	}

	/// <summary>
	/// Verifies that on an animated deck a refresh leaves no element where a card it no longer holds was, so that card
	/// leaves no gap in the deck's layout.
	/// </summary>
	[Fact]
	public async Task RefreshAsync_AnimatedDeck_LeavesNoElementForARemovedCard()
	{
		var cards = Cards();
		var deck = RenderDeck(cards, p => p.Add(x => x.IsAnimated, true));
		deck.FindAll("div[id^='pd-animation-']").Should().HaveCount(3);
		cards.RemoveAll(card => card.Name != "Bravo");

		await deck.InvokeAsync(deck.Instance.RefreshAsync);

		deck.FindAll("div[id^='pd-animation-']").Should().ContainSingle();
		Names(deck).Should().Equal("Bravo");
	}

	/// <summary>
	/// Verifies that a refresh deselects a selected card the deck no longer holds, so a later drag cannot take it along.
	/// </summary>
	[Fact]
	public async Task RefreshAsync_DeselectsACardTheDeckNoLongerHolds()
	{
		var cards = Cards();
		var deck = RenderDeck(cards, p => p.Add(x => x.MultipleSelection, true));
		await deck.InvokeAsync(() => CardElement(deck, "Alpha").MouseUpAsync(new MouseEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "Charlie").MouseUpAsync(new MouseEventArgs { CtrlKey = true }));
		cards.RemoveAll(card => card.Name == "Charlie");

		await deck.InvokeAsync(deck.Instance.RefreshAsync);

		deck.Instance.Selection.Select(card => card.Name).Should().Equal("Alpha");
	}

	/// <summary>
	/// Verifies that a selected card the data function returns as a new object, with the same id, stays selected.
	/// </summary>
	[Fact]
	public async Task RefreshAsync_KeepsASelectedCardReturnedAsANewObject()
	{
		var cards = Cards();
		var deck = Render<PDCardDeck<Card>>(parameters => parameters
			.Add(p => p.MultipleSelection, true)
			.Add(p => p.DataFunction, () => Task.FromResult(new DataResponse<Card>([.. cards.Select(CopyOf)], cards.Count))));
		deck.WaitForState(() => deck.Instance.DataLoaded);
		await deck.InvokeAsync(() => CardElement(deck, "Bravo").MouseUpAsync(new MouseEventArgs()));

		await deck.InvokeAsync(deck.Instance.RefreshAsync);

		deck.Instance.Selection.Should().ContainSingle()
			.Which.Should().BeSameAs(deck.Instance.Cards.Single(card => card.Name == "Bravo"));
		CardElement(deck, "Bravo").ClassList.Should().Contain("selected");
	}

	/// <summary>
	/// Verifies that refreshing a group reloads every deck in it.
	/// </summary>
	[Fact]
	public async Task Group_RefreshAsync_ReloadsEveryDeck()
	{
		var board = new Board();
		var group = RenderGroup(board);
		board.Decks["left"].RemoveAll(card => card.Name == "A");
		board.Decks["right"].Add(new Card("D", 1));

		await group.InvokeAsync(group.Instance.RefreshAsync);

		group.WaitForAssertion(() => Names(group, "left").Should().Equal("B"));
		Names(group, "right").Should().Equal("C", "D");
	}

	/// <summary>
	/// Verifies that a group refresh asked for while a drop is being saved waits for it, so the dropped card does not jump
	/// back to the deck it came from before the save finishes.
	/// </summary>
	[Fact]
	public async Task Group_RefreshAsync_WhileADropIsSaved_WaitsForIt()
	{
		var board = new Board(movesTheDroppedCards: true);
		var saved = new TaskCompletionSource();
		var group = RenderGroup(board, transformation: async (source, destination, cards) =>
		{
			await saved.Task;
			await board.Transform(source, destination, cards);
		});
		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragStartAsync(new DragEventArgs()));
		await group.InvokeAsync(() => group.Find("#right").DragEnterAsync(new DragEventArgs()));
		var dropping = group.InvokeAsync(Deck(group, "right").Instance.InitiateTransformAsync);

		var refreshing = group.InvokeAsync(group.Instance.RefreshAsync);

		refreshing.IsCompleted.Should().BeFalse();
		Deck(group, "right").Instance.Cards.Select(card => card.Name).Should().Equal("A", "C");
		saved.SetResult();
		await Task.WhenAll(dropping, refreshing);
		group.WaitForAssertion(() => Names(group, "right").Should().Equal("A", "C"));
		Names(group, "left").Should().Equal("B");
	}

	/// <summary>
	/// Verifies that a group refresh asked for by the group's own transformation runs at once, rather than waiting for the
	/// move it is part of, which would never finish.
	/// </summary>
	[Fact]
	public async Task Group_RefreshAsync_FromTheTransformation_DoesNotWaitForItself()
	{
		var board = new Board(movesTheDroppedCards: true);
		IRenderedComponent<PDCardDeckGroup<Card>>? group = null;
		group = RenderGroup(board, transformation: async (source, destination, cards) =>
		{
			await board.Transform(source, destination, cards);
			await group!.Instance.RefreshAsync();
		});
		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragStartAsync(new DragEventArgs()));
		await group.InvokeAsync(() => group.Find("#right").DragEnterAsync(new DragEventArgs()));

		await group.InvokeAsync(Deck(group, "right").Instance.InitiateTransformAsync)
			.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

		group.WaitForAssertion(() => Names(group, "right").Should().Equal("A", "C"));
		Names(group, "left").Should().Equal("B");
	}
}
