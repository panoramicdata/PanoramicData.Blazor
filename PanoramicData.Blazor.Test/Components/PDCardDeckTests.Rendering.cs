using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Rendering and template tests for <see cref="PDCardDeck{TCard}"/>.
/// </summary>
public partial class PDCardDeckTests
{
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
}
