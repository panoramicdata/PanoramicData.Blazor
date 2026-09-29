using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDCardDeckGroup{TCard}"/> coordinates its decks: it tracks their loading, moves
/// dragged cards between them and hands completed moves to the transformation.
/// </summary>
public class PDCardDeckGroupTests : BunitContext
{
	private readonly Dictionary<string, List<Card>> _store = new()
	{
		["todo"] = [new Card("One", 0), new Card("Two", 1), new Card("Three", 2)],
		["done"] = [new Card("Four", 0)]
	};

	private readonly ListDataProviderService<Card> _provider = new();
	private int _shows;
	private int _hides;

	/// <summary>Sets up the rendering context.</summary>
	public PDCardDeckGroupTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		var overlay = Services.GetRequiredService<IBlockOverlayService>();
		overlay.OnShow += _ => _shows++;
		overlay.OnHide += () => _hides++;
	}

	private IRenderedComponent<PDCardDeckGroup<Card>> RenderGroup(params string[] deckIds)
		=> RenderConfiguredGroup(null, deckIds);

	private IRenderedComponent<PDCardDeckGroup<Card>> RenderConfiguredGroup(
		Action<ComponentParameterCollectionBuilder<PDCardDeckGroup<Card>>>? configure,
		params string[] deckIds)
	{
		var group = Render<PDCardDeckGroup<Card>>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, _provider)
				.Add(p => p.ChildContent, builder =>
				{
					foreach (var deckId in deckIds)
					{
						builder.OpenComponent<PDCardDeck<Card>>(0);
						builder.AddAttribute(1, nameof(PDCardDeck<Card>.Id), deckId);
						builder.AddAttribute(2, nameof(PDCardDeck<Card>.DataFunction), DataFor(deckId));
						builder.AddAttribute(3, nameof(PDCardDeck<Card>.DeckTemplate), (RenderFragment<PDCardDeck<Card>>)DeckMarkup);
						builder.CloseComponent();
					}
				});
			configure?.Invoke(parameters);
		});

		if (deckIds.Length > 0)
		{
			group.WaitForAssertion(() => group.FindAll("ul.deck").Should().HaveCount(deckIds.Length));
		}

		return group;
	}

	private Func<Task<DataResponse<Card>>> DataFor(string deckId)
		=> () => Task.FromResult(new DataResponse<Card>([.. _store[deckId]], _store[deckId].Count));

	private static RenderFragment DeckMarkup(PDCardDeck<Card> deck) => builder =>
	{
		builder.OpenElement(0, "ul");
		builder.AddAttribute(1, "class", "deck");
		builder.AddAttribute(2, "data-deck", deck.Id);
		foreach (var card in deck.Cards)
		{
			builder.OpenElement(3, "li");
			builder.AddContent(4, card.Name);
			builder.CloseElement();
		}

		builder.CloseElement();
	};

	private static PDCardDeck<Card> Deck(IRenderedComponent<PDCardDeckGroup<Card>> group, string id)
		=> group.FindComponents<PDCardDeck<Card>>().Single(d => d.Instance.Id == id).Instance;

	private static Task StartDragAsync(IRenderedComponent<PDCardDeckGroup<Card>> group, string deckId, string cardName)
	{
		var deck = Deck(group, deckId);
		return group.InvokeAsync(() => deck.OnDragStartAsync(new DragEventArgs(), deck.Cards.Single(c => c.Name == cardName)));
	}

	/// <summary>
	/// Verifies that the group renders its decks inside a container with its id and the default class,
	/// and hides the block overlay once every deck has loaded.
	/// </summary>
	[Fact]
	public void LoadedDecks_AreRendered_AndTheOverlayIsHidden()
	{
		var group = RenderGroup("todo", "done");

		var container = group.Find("div.pd-carddeck-group-default");
		container.Id.Should().Be(group.Instance.Id);
		group.FindAll("ul.deck").Select(d => d.GetAttribute("data-deck")).Should().Equal("todo", "done");
		group.Instance.AllDataLoaded().Should().BeTrue();
		group.FindAll(".pd-carddeck-loading").Should().BeEmpty();
		_hides.Should().BePositive();
	}

	/// <summary>
	/// Verifies that a custom CSS class replaces the default one.
	/// </summary>
	[Fact]
	public void CssClass_ReplacesTheDefaultClass()
	{
		var group = RenderConfiguredGroup(p => p.Add(x => x.CssClass, "board"), "todo");

		group.Find("div").ClassName.Should().Be("board");
	}

	/// <summary>
	/// Verifies that a group with no loaded decks shows the loading icon, and the block overlay once the
	/// icon has become active.
	/// </summary>
	[Fact]
	public void WithNoDecks_ShowsTheLoadingIcon_AndTheOverlay()
	{
		var group = RenderGroup();

		group.Instance.AllDataLoaded().Should().BeFalse();
		group.WaitForAssertion(() => group.FindAll(".pd-carddeck-loading").Should().ContainSingle(), TimeSpan.FromSeconds(5));
		group.Render();

		_shows.Should().BePositive();
	}

	/// <summary>
	/// Verifies that dragging a card from one deck into another moves it there.
	/// </summary>
	[Fact]
	public async Task DraggingIntoAnotherDeck_MovesTheCard()
	{
		var group = RenderGroup("todo", "done");
		await StartDragAsync(group, "todo", "Two");

		await group.InvokeAsync(() => Deck(group, "done").RegisterDestination());

		Deck(group, "todo").Cards.Select(c => c.Name).Should().Equal("One", "Three");
		Deck(group, "done").Cards.Select(c => c.Name).Should().Contain("Two");
		Deck(group, "done").Selection.Select(c => c.Name).Should().Equal("Two");
	}

	/// <summary>
	/// Verifies that a move the validation function refuses leaves both decks as they were.
	/// </summary>
	[Fact]
	public async Task RefusedMove_LeavesTheDecksUnchanged()
	{
		var validations = new List<(string Source, string Destination)>();
		var group = RenderConfiguredGroup(p => p.Add(x => x.ValidateCardMove, (source, destination) =>
		{
			validations.Add((source.Id, destination.Id));
			return false;
		}), "todo", "done");
		await StartDragAsync(group, "todo", "Two");

		await group.InvokeAsync(() => Deck(group, "done").RegisterDestination());

		validations.Should().Equal(("todo", "done"));
		Deck(group, "todo").Cards.Should().HaveCount(3);
		Deck(group, "done").Cards.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that entering a deck while nothing is being dragged, or entering the same deck twice, moves nothing.
	/// </summary>
	[Fact]
	public async Task RegisterDestination_WithoutADragOrTwiceInARow_MovesNothing()
	{
		var group = RenderGroup("todo", "done");

		await group.InvokeAsync(() => Deck(group, "done").RegisterDestination());
		await StartDragAsync(group, "todo", "One");
		await group.InvokeAsync(() => Deck(group, "todo").RegisterDestination());

		Deck(group, "todo").Cards.Should().HaveCount(3);
		Deck(group, "done").Cards.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that starting a drag in one deck clears the selection held by the others.
	/// </summary>
	[Fact]
	public async Task StartingADrag_ClearsTheOtherDecksSelection()
	{
		var group = RenderGroup("todo", "done");
		await StartDragAsync(group, "done", "Four");
		Deck(group, "done").Selection.Should().ContainSingle();

		await StartDragAsync(group, "todo", "One");

		Deck(group, "done").Selection.Should().BeEmpty();
		Deck(group, "todo").Selection.Select(c => c.Name).Should().Equal("One");
	}

	/// <summary>
	/// Verifies that a drop hands the move to the transformation with the provider, the decks and the cards,
	/// with deck positions brought up to date first, then reloads the decks.
	/// </summary>
	[Fact]
	public async Task Drop_RunsTheTransformation_ThenReloadsTheDecks()
	{
		(IDataProviderService<Card> Provider, string Source, string Destination, List<string> Cards)? call = null;
		var group = RenderConfiguredGroup(p => p.Add(x => x.Transformation, (provider, source, destination, cards) =>
		{
			call = (provider, source.Id, destination.Id, [.. cards.Select(c => c.Name)]);
			_store["todo"].RemoveAll(c => c.Name == "One");
			return Task.CompletedTask;
		}), "todo", "done");
		await StartDragAsync(group, "todo", "One");

		await group.InvokeAsync(() => Deck(group, "todo").InitiateTransformAsync());

		call.Should().NotBeNull();
		call!.Value.Provider.Should().BeSameAs(_provider);
		(call.Value.Source, call.Value.Destination).Should().Be(("todo", "todo"));
		call.Value.Cards.Should().Equal("One");
		group.WaitForAssertion(() => Deck(group, "todo").Cards.Select(c => c.Name).Should().Equal("Two", "Three"));
	}

	/// <summary>
	/// Verifies that without a transformation, or with no drag in progress, a drop does nothing.
	/// </summary>
	[Fact]
	public async Task Drop_WithoutATransformationOrADrag_DoesNothing()
	{
		var calls = 0;
		var withTransformation = RenderConfiguredGroup(p => p.Add(x => x.Transformation, (_, _, _, _) =>
		{
			calls++;
			return Task.CompletedTask;
		}), "todo");
		await withTransformation.InvokeAsync(() => Deck(withTransformation, "todo").InitiateTransformAsync());

		var withoutTransformation = RenderGroup("done");
		await StartDragAsync(withoutTransformation, "done", "Four");
		await withoutTransformation.InvokeAsync(() => Deck(withoutTransformation, "done").InitiateTransformAsync());

		calls.Should().Be(0);
		Deck(withoutTransformation, "done").Cards.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that ending a drag through a deck reloads the source deck from its data.
	/// </summary>
	[Fact]
	public async Task EndDragOperation_ReloadsTheSourceDeck()
	{
		var group = RenderGroup("todo", "done");
		await StartDragAsync(group, "todo", "One");
		_store["todo"].Add(new Card("Five", 3));

		await group.InvokeAsync(() => Deck(group, "todo").EndDragOperationAsync());

		group.WaitForAssertion(() => Deck(group, "todo").Cards.Select(c => c.Name).Should().Equal("One", "Two", "Three", "Five"));
		Deck(group, "todo").DragState.IsDragging.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that disposing the group can be done more than once without error.
	/// </summary>
	[Fact]
	public void Dispose_IsRepeatable()
	{
		var group = RenderGroup("todo");

		group.Instance.Dispose();
		var again = () => group.Instance.Dispose();

		again.Should().NotThrow();
	}

	/// <summary>A card in a deck.</summary>
	public sealed class Card : ICard
	{
		/// <summary>Creates a card.</summary>
		/// <param name="name">Display name.</param>
		/// <param name="position">Position within its deck.</param>
		public Card(string name, int position)
		{
			Name = name;
			DeckPosition = position;
		}

		/// <inheritdoc />
		public Guid Id { get; set; } = Guid.NewGuid();

		/// <inheritdoc />
		public int? DeckPosition { get; set; }

		/// <summary>Gets the display name.</summary>
		public string Name { get; }
	}
}
