using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Click and drag selection tests for <see cref="PDCardDeck{TCard}"/>.
/// </summary>
public partial class PDCardDeckTests
{
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
}
