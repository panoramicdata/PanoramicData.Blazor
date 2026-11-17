using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Drag reordering tests for <see cref="PDCardDeck{TCard}"/>.
/// </summary>
public partial class PDCardDeckTests
{
	/// <summary>
	/// Verifies that dragging a card over another moves it to that card's place.
	/// </summary>
	[Fact]
	public async Task DraggingOverACard_MovesTheDraggedCardThere()
	{
		var deck = RenderDeck();

		await DragStartAsync(deck, "Alpha");
		await DragOverAsync(deck, "Charlie");

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

		await DragStartAsync(deck, "Charlie");
		await DragOverAsync(deck, "Bravo");
		await DragOverAsync(deck, "Alpha");

		Names(deck).Should().Equal("Charlie", "Alpha", "Bravo");
	}

	/// <summary>
	/// Verifies that dragging over a card while not dragging, or over the dragged card itself, moves nothing.
	/// </summary>
	[Fact]
	public async Task DragOver_WithoutADragOrOnTheSelection_MovesNothing()
	{
		var deck = RenderDeck();

		await DragOverAsync(deck, "Charlie");
		await DragStartAsync(deck, "Bravo");
		await DragOverAsync(deck, "Bravo");

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

		await DragStartAsync(deck, "A");
		await DragOverAsync(deck, "D");

		Names(deck).Should().Equal("C", "D", "A", "B");
	}

	/// <summary>
	/// Verifies that a selection made out of deck order is dragged in the order it was selected, with no
	/// adjustment for a contiguous block.
	/// </summary>
	[Fact]
	public async Task DraggingASelectionMadeOutOfOrder_MovesItInSelectionOrder()
	{
		var deck = RenderDeck([new("A", 0), new("B", 1), new("C", 2), new("D", 3)], p => p.Add(x => x.MultipleSelection, true));
		await deck.InvokeAsync(() => CardElement(deck, "C").MouseUpAsync(new MouseEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "A").MouseUpAsync(new MouseEventArgs { CtrlKey = true }));

		await DragStartAsync(deck, "A");
		await DragOverAsync(deck, "D");

		Names(deck).Should().Equal("B", "D", "C", "A");
	}

	/// <summary>
	/// Verifies that dropping on the deck ends the drag, and the script's end-of-drag notification does the same.
	/// </summary>
	[Fact]
	public async Task DropAndEndOfDrag_EndTheDrag()
	{
		var deck = RenderDeck();
		await DragStartAsync(deck, "Bravo");

		await deck.InvokeAsync(() => deck.Find($"#{deck.Instance.Id}").DropAsync(new DragEventArgs()));
		deck.Instance.DragState.IsDragging.Should().BeFalse();

		await DragStartAsync(deck, "Bravo");
		await deck.InvokeAsync(deck.Instance.EndDragOperationAsync);
		deck.Instance.DragState.IsDragging.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that an animated deck wraps each card for animation and still reorders on drag.
	/// </summary>
	[Fact]
	public async Task AnimatedDeck_StillReordersOnDrag()
	{
		var deck = RenderDeck(more: p => p.Add(x => x.IsAnimated, true));

		deck.FindComponents<PDAnimation>().Should().HaveCount(3);
		await DragStartAsync(deck, "Alpha");
		await DragOverAsync(deck, "Charlie");

		Names(deck).Should().Equal("Bravo", "Charlie", "Alpha");
	}

	/// <summary>
	/// Verifies that further dragover events on the card just passed move nothing, so the dragged card does not flash
	/// back and forth.
	/// </summary>
	[Fact]
	public async Task RepeatedDragOverTheSameCard_MovesTheDraggedCardOnce()
	{
		// Over a slow connection these are the dragovers still in flight when the reorder renders.
		var deck = RenderDeck();
		await DragStartAsync(deck, "Alpha");

		List<string> orders = [];
		for (var dragOver = 0; dragOver < 4; dragOver++)
		{
			await DragOverAsync(deck, "Bravo");
			orders.Add(string.Join(", ", Names(deck)));
		}

		orders.Should().AllBe("Bravo, Alpha, Charlie");
	}

	/// <summary>
	/// Verifies that the card just passed moves the dragged card back once the pointer has been over the dragged card.
	/// </summary>
	[Fact]
	public async Task DragOverTheCardJustPassed_AfterTheDraggedCard_MovesBack()
	{
		var deck = RenderDeck();
		await DragStartAsync(deck, "Alpha");
		await DragOverAsync(deck, "Bravo");

		await DragOverAsync(deck, "Alpha");
		await DragOverAsync(deck, "Bravo");

		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
	}

	/// <summary>
	/// Verifies that a new drag is not blocked by the card the previous drag last passed.
	/// </summary>
	[Fact]
	public async Task NewDrag_CanMovePastTheCardThePreviousDragLastPassed()
	{
		var deck = RenderDeck();
		await DragStartAsync(deck, "Alpha");
		await DragOverAsync(deck, "Bravo");
		await DragEndAsync(deck, "Alpha");

		await DragStartAsync(deck, "Alpha");
		await DragOverAsync(deck, "Bravo");

		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
	}
}
