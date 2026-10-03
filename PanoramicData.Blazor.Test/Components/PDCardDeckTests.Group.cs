using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests of <see cref="PDCardDeck{TCard}"/> decks working together in a <see cref="PDCardDeckGroup{TCard}"/>.
/// </summary>
public partial class PDCardDeckTests
{
	private sealed class Board
	{
		public Dictionary<string, List<Card>> Decks { get; } = new()
		{
			["left"] = [new("A", 0), new("B", 1)],
			["right"] = [new("C", 0)]
		};

		public List<(string Source, string Destination, List<string> Cards)> Transforms { get; } = [];

		public Func<Task<DataResponse<Card>>> DataFor(string deckId)
			=> () => Task.FromResult(new DataResponse<Card>([.. Decks[deckId]], Decks[deckId].Count));

		public Task Transform(PDCardDeck<Card> source, PDCardDeck<Card> destination, List<Card> cards)
		{
			Transforms.Add((source.Id, destination.Id, [.. cards.Select(c => c.Name)]));
			Decks[source.Id] = [.. source.Cards];
			Decks[destination.Id] = [.. destination.Cards];
			return Task.CompletedTask;
		}
	}

	private IRenderedComponent<PDCardDeckGroup<Card>> RenderGroup(Board board, Func<PDCardDeck<Card>, PDCardDeck<Card>, bool>? validate = null)
	{
		var group = Render<PDCardDeckGroup<Card>>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<Card>())
			.Add(p => p.ValidateCardMove, validate)
			.Add(p => p.Transformation, (_, source, destination, cards) => board.Transform(source, destination, cards))
			.AddChildContent<PDCardDeck<Card>>(deck => deck
				.Add(p => p.Id, "left")
				.Add(p => p.MultipleSelection, true)
				.Add(p => p.DataFunction, board.DataFor("left")))
			.AddChildContent<PDCardDeck<Card>>(deck => deck
				.Add(p => p.Id, "right")
				.Add(p => p.MultipleSelection, true)
				.Add(p => p.DataFunction, board.DataFor("right"))));
		group.WaitForAssertion(() => group.FindAll("div.card").Should().HaveCount(3));
		return group;
	}

	private static IRenderedComponent<PDCardDeck<Card>> Deck(IRenderedComponent<PDCardDeckGroup<Card>> group, string id)
		=> group.FindComponents<PDCardDeck<Card>>().Single(d => d.Instance.Id == id);

	private static List<string> Names(IRenderedComponent<PDCardDeckGroup<Card>> group, string deckId)
		=> [.. group.FindAll($"#{deckId} div.card").Select(c => c.TextContent.Trim())];

	/// <summary>
	/// Verifies that decks in a group load and show their cards without being re-rendered from outside.
	/// </summary>
	[Fact]
	public void Group_DecksShowTheirCards()
	{
		var group = RenderGroup(new Board());

		Names(group, "left").Should().Equal("A", "B");
		Names(group, "right").Should().Equal("C");
	}

	/// <summary>
	/// Verifies that selecting in one deck of a group clears the selection in the others.
	/// </summary>
	[Fact]
	public async Task Group_SelectingInOneDeck_ClearsTheOthers()
	{
		var group = RenderGroup(new Board());

		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].MouseUpAsync(new MouseEventArgs()));
		await group.InvokeAsync(() => group.FindAll("#right div.card")[0].MouseUpAsync(new MouseEventArgs()));

		Deck(group, "left").Instance.Selection.Should().BeEmpty();
		Deck(group, "right").Instance.Selection.Select(c => c.Name).Should().Equal("C");
	}

	/// <summary>
	/// Verifies that dragging a card into another deck moves it there, and the drop hands the move to the transformation.
	/// </summary>
	[Fact]
	public async Task Group_DraggingIntoAnotherDeck_MovesTheCardAndTransforms()
	{
		var board = new Board();
		var group = RenderGroup(board);

		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragStartAsync(new DragEventArgs()));
		await group.InvokeAsync(() => group.Find("#right").DragEnterAsync(new DragEventArgs()));

		Deck(group, "right").Instance.Cards.Select(c => c.Name).Should().Equal("A", "C");
		Deck(group, "left").Instance.Cards.Select(c => c.Name).Should().Equal("B");

		// Dropped straight after entering the destination, with no further drag-enter in between (#175).
		await group.InvokeAsync(Deck(group, "right").Instance.InitiateTransformAsync);

		board.Transforms.Should().ContainSingle();
		board.Transforms[0].Source.Should().Be("left");
		board.Transforms[0].Destination.Should().Be("right");
		board.Transforms[0].Cards.Should().Equal("A");
		board.Decks["right"].Select(c => c.DeckPosition).Should().Equal(0, 1);
		group.WaitForAssertion(() => Names(group, "right").Should().Equal("A", "C"));
		Names(group, "left").Should().Equal("B");
	}

	/// <summary>
	/// Verifies that a move the group's validation rejects leaves both decks as they were.
	/// </summary>
	[Fact]
	public async Task Group_RejectedMove_LeavesTheDecksAlone()
	{
		var group = RenderGroup(new Board(), (_, _) => false);

		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragStartAsync(new DragEventArgs()));
		await group.InvokeAsync(() => group.Find("#right").DragEnterAsync(new DragEventArgs()));

		Deck(group, "left").Instance.Cards.Select(c => c.Name).Should().Equal("A", "B");
		Deck(group, "right").Instance.Cards.Select(c => c.Name).Should().Equal("C");
	}

	/// <summary>
	/// Verifies that the end of a drag in a group reloads the decks from their data.
	/// </summary>
	[Fact]
	public async Task Group_EndOfDrag_ReloadsTheDecks()
	{
		var board = new Board();
		var group = RenderGroup(board);

		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragStartAsync(new DragEventArgs()));
		board.Decks["left"].Add(new Card("D", 2));
		await group.InvokeAsync(Deck(group, "left").Instance.EndDragOperationAsync);

		Deck(group, "left").Instance.DragState.IsDragging.Should().BeFalse();
		group.WaitForAssertion(() => Names(group, "left").Should().Equal("A", "B", "D"));
	}
}
