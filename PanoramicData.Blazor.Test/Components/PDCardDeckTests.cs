using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDCardDeck{TCard}"/>: loading, rendering, selection, reordering by drag, and moving cards between decks in a group.
/// </summary>
public partial class PDCardDeckTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDCardDeck.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDCardDeckTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private static List<Card> Cards() => [new("Charlie", 2), new("Alpha", 0), new("Bravo", 1)];

	private IRenderedComponent<PDCardDeck<Card>> RenderDeck(
		List<Card>? cards = null,
		Action<ComponentParameterCollectionBuilder<PDCardDeck<Card>>>? more = null)
	{
		var source = cards ?? Cards();
		var deck = Render<PDCardDeck<Card>>(parameters =>
		{
			parameters.Add(p => p.DataFunction, () => Task.FromResult(new DataResponse<Card>(source, source.Count)));
			more?.Invoke(parameters);
		});
		deck.WaitForState(() => deck.Instance.DataLoaded);
		return deck;
	}

	private static List<string> Names(IRenderedComponent<PDCardDeck<Card>> deck)
		=> [.. deck.FindAll("div.card").Select(c => c.TextContent.Trim())];

	private static AngleSharp.Dom.IElement CardElement(IRenderedComponent<PDCardDeck<Card>> deck, string name)
		=> deck.FindAll("div.card").Single(c => c.TextContent.Trim() == name);

	/// <summary>
	/// Verifies that the deck loads its cards through the data function and shows them in deck position order.
	/// </summary>
	[Fact]
	public void Cards_AreShownInDeckPositionOrder()
	{
		var deck = RenderDeck();

		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
		deck.Instance.Cards.Select(c => c.Name).Should().Equal("Alpha", "Bravo", "Charlie");
		deck.FindAll(".pd-carddeck-loading").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a deck outside a group shows its cards once they have loaded, without anything else
	/// forcing it to render again (#175, #204).
	/// </summary>
	[Fact]
	public void Standalone_ShowsItsCards_WithoutAnExtraRender()
	{
		var cards = Cards();
		var deck = Render<PDCardDeck<Card>>(parameters => parameters
			.Add(p => p.DataFunction, () => Task.FromResult(new DataResponse<Card>(cards, cards.Count))));

		deck.WaitForAssertion(() => deck.FindAll("div.card").Should().HaveCount(cards.Count), TimeSpan.FromSeconds(10));
		deck.Instance.DataLoaded.Should().BeTrue();
		deck.FindAll(".pd-carddeck-loading").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that each card uses the default card class, and the deck the default deck class and medium size.
	/// </summary>
	[Fact]
	public void Defaults_UseTheDefaultClasses()
	{
		var deck = RenderDeck();

		var element = deck.Find($"#{deck.Instance.Id}");
		element.ClassList.Should().Contain("pdcarddeck").And.Contain("md").And.NotContain("disabled");
		deck.FindAll("div.card").Should().AllSatisfy(c => c.ClassList.Should().Contain("card-default"));
		deck.FindAll("div.card").Should().AllSatisfy(c => c.GetAttribute("draggable").Should().Be("true"));
	}

	/// <summary>
	/// Verifies that the deck's size, CSS class and disabled state are reflected on the deck and its cards.
	/// </summary>
	[Theory]
	[InlineData(ButtonSizes.Small, "sm")]
	[InlineData(ButtonSizes.Large, "lg")]
	public void SizeCssAndDisabled_AreReflected(ButtonSizes size, string sizeClass)
	{
		var deck = RenderDeck(more: p => p
			.Add(x => x.Size, size)
			.Add(x => x.CssClass, "my-deck")
			.Add(x => x.IsEnabled, false));

		var element = deck.Find($"#{deck.Instance.Id}");
		element.ClassList.Should().Contain("my-deck").And.Contain(sizeClass).And.Contain("disabled");
		deck.FindAll("div.card").Should().AllSatisfy(c => c.GetAttribute("draggable").Should().Be("false"));
	}

	/// <summary>
	/// Verifies that the card CSS function and card template decide how each card looks.
	/// </summary>
	[Fact]
	public void CardCssAndTemplate_DecideHowEachCardLooks()
	{
		var deck = RenderDeck(more: p => p
			.Add(x => x.CardCss, card => $"card-{card.Name.ToLowerInvariant()}")
			.Add(x => x.CardTemplate, card => builder => builder.AddMarkupContent(0, $"<b>{card.Name}!</b>")));

		CardElement(deck, "Alpha!").ClassList.Should().Contain("card-alpha").And.NotContain("card-default");
		deck.FindAll("div.card b").Should().HaveCount(3);
	}

	/// <summary>
	/// Verifies that a deck template replaces the card rendering entirely and is given the deck.
	/// </summary>
	[Fact]
	public void DeckTemplate_ReplacesTheCards()
	{
		var deck = RenderDeck(more: p => p
			.Add(x => x.DeckTemplate, d => builder => builder.AddMarkupContent(0, $"<p class=\"summary\">{d.Cards.Count} cards</p>")));

		deck.FindAll("div.card").Should().BeEmpty();
		deck.Find(".summary").TextContent.Should().Be("3 cards");
	}

	/// <summary>
	/// Verifies that the deck registers its drag listeners with the script, passing a reference back to itself.
	/// </summary>
	[Fact]
	public void FirstRender_RegistersTheDragListeners()
	{
		var module = JSInterop.SetupModule(ModulePath);

		RenderDeck();

		module.VerifyInvoke("registerValidDragOperationListeners").Arguments[1]
			.Should().BeOfType<DotNetObjectReference<PDCardDeck<Card>>>();
		module.VerifyInvoke("registerInvalidDragOperationListeners");
	}

	/// <summary>
	/// Verifies that without multiple selection a click selects nothing.
	/// </summary>
	[Fact]
	public async Task Click_WithoutMultipleSelection_SelectsNothing()
	{
		var deck = RenderDeck();

		await deck.InvokeAsync(() => CardElement(deck, "Bravo").MouseUpAsync(new MouseEventArgs()));

		deck.Instance.Selection.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that with multiple selection a click selects one card, Ctrl toggles and Shift selects a run.
	/// </summary>
	[Fact]
	public async Task Click_WithMultipleSelection_SelectsOneTogglesOrSelectsARun()
	{
		var deck = RenderDeck(more: p => p.Add(x => x.MultipleSelection, true));

		await deck.InvokeAsync(() => CardElement(deck, "Alpha").MouseUpAsync(new MouseEventArgs()));
		deck.Instance.Selection.Select(c => c.Name).Should().Equal("Alpha");
		CardElement(deck, "Alpha").ClassList.Should().Contain("selected");

		await deck.InvokeAsync(() => CardElement(deck, "Charlie").MouseUpAsync(new MouseEventArgs { CtrlKey = true }));
		deck.Instance.Selection.Select(c => c.Name).Should().Equal("Alpha", "Charlie");

		await deck.InvokeAsync(() => CardElement(deck, "Alpha").MouseUpAsync(new MouseEventArgs { CtrlKey = true }));
		deck.Instance.Selection.Select(c => c.Name).Should().Equal("Charlie");

		await deck.InvokeAsync(() => CardElement(deck, "Alpha").MouseUpAsync(new MouseEventArgs { ShiftKey = true }));
		deck.Instance.Selection.Select(c => c.Name).Should().Equal("Alpha", "Bravo", "Charlie");
	}

	/// <summary>
	/// Verifies that Shift with no earlier click selects from the first card.
	/// </summary>
	[Fact]
	public async Task ShiftClick_WithNoEarlierClick_SelectsFromTheStart()
	{
		var deck = RenderDeck(more: p => p.Add(x => x.MultipleSelection, true));

		await deck.InvokeAsync(() => CardElement(deck, "Bravo").MouseUpAsync(new MouseEventArgs { ShiftKey = true }));

		deck.Instance.Selection.Select(c => c.Name).Should().Equal("Alpha", "Bravo");
	}

	/// <summary>
	/// Verifies that starting to drag a card selects it and marks it as dragging, and ending the drag clears that.
	/// </summary>
	[Fact]
	public async Task Dragging_SelectsAndMarksTheCard()
	{
		var deck = RenderDeck();

		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragStartAsync(new DragEventArgs()));

		deck.Instance.DragState.IsDragging.Should().BeTrue();
		deck.Instance.Selection.Select(c => c.Name).Should().Equal("Bravo");
		CardElement(deck, "Bravo").ClassList.Should().Contain("dragging").And.Contain("selected");

		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragEndAsync(new DragEventArgs()));

		deck.Instance.DragState.IsDragging.Should().BeFalse();
		deck.Instance.DragState.TargetIndex.Should().Be(-1);
	}

	/// <summary>
	/// Verifies that dragging a card that is part of a multiple selection keeps the whole selection.
	/// </summary>
	[Fact]
	public async Task DraggingASelectedCard_KeepsTheSelection()
	{
		var deck = RenderDeck(more: p => p.Add(x => x.MultipleSelection, true));
		await deck.InvokeAsync(() => CardElement(deck, "Alpha").MouseUpAsync(new MouseEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "Bravo").MouseUpAsync(new MouseEventArgs { CtrlKey = true }));

		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragStartAsync(new DragEventArgs()));

		deck.Instance.Selection.Select(c => c.Name).Should().Equal("Alpha", "Bravo");
	}

	/// <summary>
	/// Verifies that dragging a card over another moves it to that card's place.
	/// </summary>
	[Fact]
	public async Task DraggingOverACard_MovesTheDraggedCardThere()
	{
		var deck = RenderDeck();

		await deck.InvokeAsync(() => CardElement(deck, "Alpha").DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "Charlie").DragOverAsync(new DragEventArgs()));

		Names(deck).Should().Equal("Bravo", "Charlie", "Alpha");
		deck.Instance.DragState.TargetIndex.Should().Be(2);
	}

	/// <summary>
	/// Verifies that dragging a card back up the deck moves it up.
	/// </summary>
	[Fact]
	public async Task DraggingUpTheDeck_MovesTheCardUp()
	{
		var deck = RenderDeck();

		await deck.InvokeAsync(() => CardElement(deck, "Charlie").DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragOverAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "Alpha").DragOverAsync(new DragEventArgs()));

		Names(deck).Should().Equal("Charlie", "Alpha", "Bravo");
	}

	/// <summary>
	/// Verifies that dragging over a card while not dragging, or over the dragged card itself, moves nothing.
	/// </summary>
	[Fact]
	public async Task DragOver_WithoutADragOrOnTheSelection_MovesNothing()
	{
		var deck = RenderDeck();

		await deck.InvokeAsync(() => CardElement(deck, "Charlie").DragOverAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragOverAsync(new DragEventArgs()));

		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
	}

	/// <summary>
	/// Verifies that a contiguous multiple selection dragged down the deck moves as a block.
	/// </summary>
	[Fact]
	public async Task DraggingAContiguousSelectionDown_MovesItAsABlock()
	{
		var deck = RenderDeck([new("A", 0), new("B", 1), new("C", 2), new("D", 3)], p => p.Add(x => x.MultipleSelection, true));
		await deck.InvokeAsync(() => CardElement(deck, "A").MouseUpAsync(new MouseEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "B").MouseUpAsync(new MouseEventArgs { ShiftKey = true }));

		await deck.InvokeAsync(() => CardElement(deck, "A").DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "D").DragOverAsync(new DragEventArgs()));

		Names(deck).Should().Equal("C", "D", "A", "B");
	}

	/// <summary>
	/// Verifies that dropping on the deck ends the drag, and the script's end-of-drag notification does the same.
	/// </summary>
	[Fact]
	public async Task DropAndEndOfDrag_EndTheDrag()
	{
		var deck = RenderDeck();
		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragStartAsync(new DragEventArgs()));

		await deck.InvokeAsync(() => deck.Find($"#{deck.Instance.Id}").DropAsync(new DragEventArgs()));
		deck.Instance.DragState.IsDragging.Should().BeFalse();

		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(deck.Instance.EndDragOperationAsync);
		deck.Instance.DragState.IsDragging.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that the group-only notifications from the script are harmless on a deck with no group.
	/// </summary>
	[Fact]
	public async Task GroupNotifications_WithoutAGroup_ChangeNothing()
	{
		var deck = RenderDeck();

		await deck.InvokeAsync(deck.Instance.RegisterDestination);
		await deck.InvokeAsync(deck.Instance.InitiateTransformAsync);

		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
	}

	/// <summary>
	/// Verifies that an animated deck wraps each card for animation and still reorders on drag.
	/// </summary>
	[Fact]
	public async Task AnimatedDeck_StillReordersOnDrag()
	{
		var deck = RenderDeck(more: p => p.Add(x => x.IsAnimated, true));

		deck.FindComponents<PDAnimation>().Should().HaveCount(3);
		await deck.InvokeAsync(() => CardElement(deck, "Alpha").DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "Charlie").DragOverAsync(new DragEventArgs()));

		Names(deck).Should().Equal("Bravo", "Charlie", "Alpha");
	}

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

	private sealed class Card(string name, int? position) : ICard
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public int? DeckPosition { get; set; } = position;

		public string Name { get; } = name;

		public override string ToString() => Name;
	}
}
