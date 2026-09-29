using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDCard{TCard}"/> renders its card inside a <see cref="PDCardDeck{TCard}"/>, reflects
/// selection and dragging in its CSS classes, and forwards pointer and drag events to its deck.
/// </summary>
public class PDCardTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDCardTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Verifies that a card with no template shows its text with the default card classes.</summary>
	[Fact]
	public void A_card_without_a_template_shows_its_text()
	{
		var deck = RenderDeck(Cards("One", "Two"));

		var cards = deck.FindAll(".card");
		cards.Select(c => c.TextContent.Trim()).Should().Equal("One", "Two");
		cards[0].ClassList.Should().Contain("card-default");
		cards[0].GetAttribute("draggable").Should().Be("true");
	}

	/// <summary>Verifies that the deck's cards are shown in deck position order.</summary>
	[Fact]
	public void Cards_are_shown_in_deck_position_order()
	{
		var cards = Cards("One", "Two", "Three");
		cards[0].DeckPosition = 2;
		cards[2].DeckPosition = 0;

		var deck = RenderDeck(cards);

		deck.FindAll(".card").Select(c => c.TextContent.Trim()).Should().Equal("Three", "Two", "One");
	}

	/// <summary>Verifies that a card template and a CSS function are both applied to each card.</summary>
	[Fact]
	public void A_template_and_css_function_are_applied()
	{
		var deck = RenderDeck(Cards("One"), parameters => parameters
			.Add(p => p.CardTemplate, card => (RenderFragment)(b => b.AddMarkupContent(0, $"<b>{card.Name}</b>")))
			.Add(p => p.CardCss, card => $"theme-{card.Name}"));

		var element = deck.Find(".card");
		element.ClassList.Should().Contain("theme-One").And.NotContain("card-default");
		element.QuerySelector("b")!.TextContent.Should().Be("One");
	}

	/// <summary>Verifies that a disabled deck renders cards that cannot be dragged.</summary>
	[Fact]
	public void A_disabled_deck_renders_undraggable_cards()
	{
		var deck = RenderDeck(Cards("One"), parameters => parameters.Add(p => p.IsEnabled, false));

		deck.Find(".card").GetAttribute("draggable").Should().Be("false");
	}

	/// <summary>Verifies that releasing the mouse on a card selects it when the deck allows selection.</summary>
	[Fact]
	public async Task Releasing_the_mouse_selects_the_card()
	{
		var deck = RenderDeck(Cards("One", "Two"), parameters => parameters.Add(p => p.MultipleSelection, true));

		await deck.InvokeAsync(() => deck.FindAll(".card")[1].MouseUpAsync(new MouseEventArgs()));

		deck.FindAll(".card").Select(c => c.ClassList.Contains("selected")).Should().Equal(false, true);
	}

	/// <summary>Verifies that a deck without selection enabled leaves a released card unselected.</summary>
	[Fact]
	public async Task Without_selection_a_released_card_stays_unselected()
	{
		var deck = RenderDeck(Cards("One"));

		await deck.InvokeAsync(() => deck.Find(".card").MouseUpAsync(new MouseEventArgs()));

		deck.Find(".card").ClassList.Should().NotContain("selected");
	}

	/// <summary>Verifies that starting a drag marks the card as selected and dragging, and ending it clears dragging.</summary>
	[Fact]
	public async Task Dragging_a_card_marks_it_until_the_drag_ends()
	{
		var deck = RenderDeck(Cards("One", "Two"));

		await deck.InvokeAsync(() => deck.FindAll(".card")[0].DragStartAsync(new DragEventArgs()));
		deck.FindAll(".card")[0].ClassList.Should().Contain("selected").And.Contain("dragging");

		await deck.InvokeAsync(() => deck.FindAll(".card")[0].DragEndAsync(new DragEventArgs()));
		deck.FindAll(".card")[0].ClassList.Should().NotContain("dragging");
	}

	/// <summary>Verifies that dragging a card over another moves it to that card's position.</summary>
	[Fact]
	public async Task Dragging_over_another_card_moves_the_dragged_card()
	{
		var deck = RenderDeck(Cards("One", "Two", "Three"));

		await deck.InvokeAsync(() => deck.FindAll(".card")[0].DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => deck.FindAll(".card")[2].DragOverAsync(new DragEventArgs()));

		deck.WaitForAssertion(() => deck.FindAll(".card").Select(c => c.TextContent.Trim())
			.Should().Equal("Two", "Three", "One"));
	}

	/// <summary>Verifies that dragging over a card with no drag in progress moves nothing.</summary>
	[Fact]
	public async Task Dragging_over_without_a_drag_moves_nothing()
	{
		var deck = RenderDeck(Cards("One", "Two"));

		await deck.InvokeAsync(() => deck.FindAll(".card")[1].DragOverAsync(new DragEventArgs()));

		deck.FindAll(".card").Select(c => c.TextContent.Trim()).Should().Equal("One", "Two");
	}

	/// <summary>Verifies that an animated card is wrapped in an animation element.</summary>
	[Fact]
	public void An_animated_card_is_wrapped_for_animation()
	{
		var deck = RenderDeck(Cards("One"), parameters => parameters.Add(p => p.IsAnimated, true));

		var card = deck.Find(".card");
		card.ParentElement!.Id.Should().StartWith("pd-animation-");
		card.TextContent.Trim().Should().Be("One");
	}

	/// <summary>Verifies that an animated card takes a template like any other.</summary>
	[Fact]
	public void An_animated_card_uses_its_template()
	{
		var deck = RenderDeck(Cards("One"), parameters => parameters
			.Add(p => p.IsAnimated, true)
			.Add(p => p.CardTemplate, card => (RenderFragment)(b => b.AddMarkupContent(0, $"<i>{card.Name}</i>"))));

		deck.Find(".card i").TextContent.Should().Be("One");
	}

	/// <summary>Verifies that dragging over a card in an animated deck still moves the dragged card.</summary>
	[Fact]
	public async Task Dragging_over_in_an_animated_deck_moves_the_dragged_card()
	{
		var deck = RenderDeck(Cards("One", "Two", "Three"), parameters => parameters.Add(p => p.IsAnimated, true));

		await deck.InvokeAsync(() => deck.FindAll(".card")[0].DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => deck.FindAll(".card")[2].DragOverAsync(new DragEventArgs()));

		deck.WaitForAssertion(() => deck.FindAll(".card").Select(c => c.TextContent.Trim())
			.Should().Equal("Two", "Three", "One"));
	}

	private IRenderedComponent<PDCardDeck<Card>> RenderDeck(
		List<Card> cards,
		Action<ComponentParameterCollectionBuilder<PDCardDeck<Card>>>? configure = null)
	{
		var deck = Render<PDCardDeck<Card>>(parameters =>
		{
			parameters.Add(p => p.DataFunction, () => Task.FromResult(new DataResponse<Card>(cards, cards.Count)));
			configure?.Invoke(parameters);
		});
		// A deck outside a group loads its cards after first render but does not re-render itself once they
		// arrive (reported separately as a PDCardDeck defect), so render it again once the data is in.
		deck.WaitForState(() => deck.Instance.DataLoaded, TimeSpan.FromSeconds(10));
		deck.Render();
		deck.FindAll(".card").Should().HaveCount(cards.Count);
		return deck;
	}

	private static List<Card> Cards(params string[] names)
		=> [.. names.Select((name, index) => new Card { Name = name, DeckPosition = index })];

	/// <summary>A card identified by name.</summary>
	public sealed class Card : ICard
	{
		/// <inheritdoc />
		public Guid Id { get; set; } = Guid.NewGuid();

		/// <inheritdoc />
		public int? DeckPosition { get; set; }

		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <inheritdoc />
		public override string ToString() => Name;
	}
}
