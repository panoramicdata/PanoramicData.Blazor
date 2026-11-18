using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests of <see cref="PDCardDeck{TCard}.KeyboardReorder"/> and <see cref="PDCardDeck{TCard}.DeleteRequested"/>.
/// </summary>
public partial class PDCardDeckTests
{
	private static KeyboardEventArgs AltUp => new() { Key = "ArrowUp", AltKey = true };

	private static KeyboardEventArgs AltDown => new() { Key = "ArrowDown", AltKey = true };

	private static KeyboardEventArgs Delete => new() { Key = "Delete" };

	private IRenderedComponent<PDCardDeck<Card>> RenderKeyboardDeck(
		List<IReadOnlyList<Card>> reorders,
		List<IReadOnlyList<Card>> deletes,
		List<Card>? cards = null)
		=> RenderDeck(cards, p => p
			.Add(x => x.MultipleSelection, true)
			.Add(x => x.KeyboardReorder, true)
			.Add(x => x.CardsReordered, reordered => reorders.Add(reordered))
			.Add(x => x.DeleteRequested, deleted => deletes.Add(deleted)));

	private static Task ClickAsync(IRenderedComponent<PDCardDeck<Card>> deck, string card, MouseEventArgs args)
		=> deck.InvokeAsync(() => CardElement(deck, card).MouseUpAsync(args));

	private static Task PressAsync(IRenderedComponent<PDCardDeck<Card>> deck, KeyboardEventArgs key)
		=> deck.InvokeAsync(() => deck.Find($"#{deck.Instance.Id}").KeyDownAsync(key));

	/// <summary>
	/// Verifies that Delete hands the selected cards to the host and leaves the deck as it was.
	/// </summary>
	[Fact]
	public async Task Delete_WithCardsSelected_RaisesDeleteRequestedAndRemovesNothing()
	{
		List<IReadOnlyList<Card>> reorders = [];
		List<IReadOnlyList<Card>> deletes = [];
		var deck = RenderKeyboardDeck(reorders, deletes);
		await ClickAsync(deck, "Bravo", new MouseEventArgs());
		await ClickAsync(deck, "Charlie", new MouseEventArgs { CtrlKey = true });

		await PressAsync(deck, Delete);

		deletes.Should().ContainSingle();
		deletes[0].Select(c => c.Name).Should().Equal("Bravo", "Charlie");
		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
		reorders.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that Delete with nothing selected raises nothing.
	/// </summary>
	[Fact]
	public async Task Delete_WithNothingSelected_RaisesNothing()
	{
		List<IReadOnlyList<Card>> deletes = [];
		var deck = RenderKeyboardDeck([], deletes);

		await PressAsync(deck, Delete);

		deletes.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that Alt+Up and Alt+Down move the selected card one place and report each new order with positions set.
	/// </summary>
	[Fact]
	public async Task AltArrows_MoveTheSelectedCardAndReportTheOrder()
	{
		List<IReadOnlyList<Card>> reorders = [];
		var deck = RenderKeyboardDeck(reorders, []);
		await ClickAsync(deck, "Charlie", new MouseEventArgs());

		await PressAsync(deck, AltUp);
		Names(deck).Should().Equal("Alpha", "Charlie", "Bravo");
		reorders.Should().ContainSingle();
		reorders[0].Select(c => c.Name).Should().Equal("Alpha", "Charlie", "Bravo");
		reorders[0].Select(c => c.DeckPosition).Should().Equal(0, 1, 2);

		await PressAsync(deck, AltDown);
		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
		reorders.Should().HaveCount(2);
		reorders[1].Select(c => c.Name).Should().Equal("Alpha", "Bravo", "Charlie");
	}

	/// <summary>
	/// Verifies that a selected run of cards moves up and down as a block.
	/// </summary>
	[Fact]
	public async Task AltArrows_MoveASelectedRunAsABlock()
	{
		var deck = RenderKeyboardDeck([], [], [new("A", 0), new("B", 1), new("C", 2), new("D", 3)]);
		await ClickAsync(deck, "B", new MouseEventArgs());
		await ClickAsync(deck, "C", new MouseEventArgs { ShiftKey = true });

		await PressAsync(deck, AltUp);
		Names(deck).Should().Equal("B", "C", "A", "D");

		await PressAsync(deck, AltDown);
		await PressAsync(deck, AltDown);
		Names(deck).Should().Equal("A", "D", "B", "C");
	}

	/// <summary>
	/// Verifies that selected cards with a gap between them each move down one place.
	/// </summary>
	[Fact]
	public async Task AltDown_MovesSelectedCardsWithAGapEachOnePlace()
	{
		var deck = RenderKeyboardDeck([], [], [new("A", 0), new("B", 1), new("C", 2), new("D", 3)]);
		await ClickAsync(deck, "A", new MouseEventArgs());
		await ClickAsync(deck, "C", new MouseEventArgs { CtrlKey = true });

		await PressAsync(deck, AltDown);

		Names(deck).Should().Equal("B", "A", "D", "C");
	}

	/// <summary>
	/// Verifies that nothing moves, and nothing is reported, once a selected card is at the end it is moving towards.
	/// </summary>
	[Fact]
	public async Task AltArrows_AtTheEnd_MoveNothing()
	{
		List<IReadOnlyList<Card>> reorders = [];
		var deck = RenderKeyboardDeck(reorders, []);
		await ClickAsync(deck, "Alpha", new MouseEventArgs());
		await ClickAsync(deck, "Charlie", new MouseEventArgs { CtrlKey = true });

		await PressAsync(deck, AltUp);
		await PressAsync(deck, AltDown);

		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
		reorders.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that an arrow without Alt, or Alt+arrow with keyboard reorder off, moves nothing.
	/// </summary>
	[Fact]
	public async Task Arrows_WithoutAltOrWithKeyboardReorderOff_MoveNothing()
	{
		List<IReadOnlyList<Card>> reorders = [];
		var deck = RenderKeyboardDeck(reorders, []);
		await ClickAsync(deck, "Charlie", new MouseEventArgs());
		await PressAsync(deck, new KeyboardEventArgs { Key = "ArrowUp" });

		var off = RenderDeck(more: p => p
			.Add(x => x.MultipleSelection, true)
			.Add(x => x.CardsReordered, reordered => reorders.Add(reordered))
			.Add(x => x.DeleteRequested, _ => { }));
		await ClickAsync(off, "Charlie", new MouseEventArgs());
		await PressAsync(off, AltUp);

		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
		Names(off).Should().Equal("Alpha", "Bravo", "Charlie");
		reorders.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a disabled deck ignores Delete and Alt+arrows.
	/// </summary>
	[Fact]
	public async Task DisabledDeck_IgnoresTheKeys()
	{
		List<IReadOnlyList<Card>> reorders = [];
		List<IReadOnlyList<Card>> deletes = [];
		var deck = RenderKeyboardDeck(reorders, deletes);
		await ClickAsync(deck, "Charlie", new MouseEventArgs());
		deck.Render(p => p.Add(x => x.IsEnabled, false));

		await PressAsync(deck, AltUp);
		await PressAsync(deck, Delete);

		reorders.Should().BeEmpty();
		deletes.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a deck takes keyboard focus only when a key does something there: with keyboard reorder outside a
	/// group, or a Delete handler.
	/// </summary>
	[Fact]
	public void Deck_TakesFocusOnlyWhenAKeyDoesSomething()
	{
		string? TabIndex(IRenderedComponent<PDCardDeck<Card>> deck) => deck.Find($"#{deck.Instance.Id}").GetAttribute("tabindex");

		TabIndex(RenderDeck()).Should().BeNull();
		TabIndex(RenderDeck(more: p => p.Add(x => x.KeyboardReorder, true))).Should().Be("0");
		TabIndex(RenderDeck(more: p => p.Add(x => x.DeleteRequested, _ => { }))).Should().Be("0");

		var board = new Board();
		var group = Render<PDCardDeckGroup<Card>>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<Card>())
			.AddChildContent<PDCardDeck<Card>>(deck => deck
				.Add(p => p.Id, "left")
				.Add(p => p.KeyboardReorder, true)
				.Add(p => p.DataFunction, board.DataFor("left"))));
		group.WaitForAssertion(() => group.FindAll("div.card").Should().HaveCount(2));

		TabIndex(Deck(group, "left")).Should().BeNull();
	}
}
