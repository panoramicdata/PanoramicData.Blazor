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
	/// Verifies that a selection made out of deck order is dragged in the order it was selected, with no
	/// adjustment for a contiguous block.
	/// </summary>
	[Fact]
	public async Task DraggingASelectionMadeOutOfOrder_MovesItInSelectionOrder()
	{
		var deck = RenderDeck([new("A", 0), new("B", 1), new("C", 2), new("D", 3)], p => p.Add(x => x.MultipleSelection, true));
		await deck.InvokeAsync(() => CardElement(deck, "C").MouseUpAsync(new MouseEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "A").MouseUpAsync(new MouseEventArgs { CtrlKey = true }));

		await deck.InvokeAsync(() => CardElement(deck, "A").DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, "D").DragOverAsync(new DragEventArgs()));

		Names(deck).Should().Equal("B", "D", "C", "A");
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
}
