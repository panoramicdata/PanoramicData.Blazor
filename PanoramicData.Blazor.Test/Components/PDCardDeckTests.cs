using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

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
	/// Verifies that each rendered card registers itself with the deck through <see cref="PDCardDeck{TCard}.Ref"/>,
	/// which then reads back as the card registered last.
	/// </summary>
	[Fact]
	public void Ref_ReadsBackTheCardRegisteredLast()
	{
		var deck = RenderDeck();

		deck.Instance.Ref.Should().BeOneOf(deck.FindComponents<PDCard<Card>>().Select(c => c.Instance));
	}

	/// <summary>
	/// Verifies that reading <see cref="PDCardDeck{TCard}.Ref"/> before any card is registered says so, rather
	/// than returning nothing.
	/// </summary>
	[Fact]
	public void Ref_BeforeAnyCardIsRegistered_Throws()
	{
		var deck = RenderDeck(more: p => p
			.Add(x => x.DeckTemplate, d => builder => builder.AddMarkupContent(0, "<p>no cards</p>")));

		var act = () => deck.Instance.Ref;

		act.Should().Throw<InvalidOperationException>();
	}

	/// <summary>
	/// Verifies that every deck gets its own default id, whatever its card type.
	/// </summary>
	[Fact]
	public void DefaultIds_AreUniqueAcrossCardTypes()
	{
		var first = RenderDeck();
		var other = Render<PDCardDeck<OtherCard>>(parameters => parameters
			.Add(p => p.DataFunction, () => Task.FromResult(new DataResponse<OtherCard>([], 0))));

		first.Instance.Id.Should().StartWith("pd-carddeck-");
		other.Instance.Id.Should().StartWith("pd-carddeck-").And.NotBe(first.Instance.Id);
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

	private sealed class Card(string name, int? position) : ICard
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public int? DeckPosition { get; set; } = position;

		public string Name { get; } = name;

		public override string ToString() => Name;
	}

	private sealed class OtherCard : ICard
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public int? DeckPosition { get; set; }
	}
}
