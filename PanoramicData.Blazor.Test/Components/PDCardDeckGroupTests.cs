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
public partial class PDCardDeckGroupTests : BunitContext
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
