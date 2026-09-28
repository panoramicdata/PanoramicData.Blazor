using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="PDCardDeckSelectionHelper{TCard}"/>.</summary>
public class PDCardDeckSelectionHelperTests
{
	private readonly PDCardDeckSelectionHelper<Card> _helper = new();
	private readonly List<Card> _deck = [.. Enumerable.Range(0, 5).Select(i => new Card { Name = $"c{i}" })];

	private static string[] Names(IEnumerable<Card> cards) => [.. cards.Select(c => c.Name)];

	/// <summary>A plain click replaces the selection with the clicked card.</summary>
	[Fact]
	public void HandleSingleSelect_ReplacesSelection()
	{
		List<Card> selection = [_deck[0], _deck[1]];

		var result = _helper.HandleSingleSelect(selection, _deck[3]);

		Names(result).Should().Equal("c3");
		result.Should().NotBeSameAs(selection);
	}

	/// <summary>A control click adds an unselected card and removes a selected one.</summary>
	[Fact]
	public void HandleIndividualAddRemove_TogglesCard()
	{
		List<Card> selection = [_deck[0]];

		Names(_helper.HandleIndividualAddRemove(selection, _deck[2])).Should().Equal("c0", "c2");
		Names(_helper.HandleIndividualAddRemove(selection, _deck[0])).Should().Equal("c2");
	}

	/// <summary>Without a pivot, a shift click selects from the first card to the clicked card.</summary>
	[Fact]
	public void HandleAddRange_WithoutPivot_SelectsFromStart()
	{
		var result = _helper.HandleAddRange([], _deck, _deck[2]);

		Names(result).Should().Equal("c0", "c1", "c2");
	}

	/// <summary>A shift click selects the range between the pivot and the clicked card, in either direction.</summary>
	[Fact]
	public void HandleAddRange_FromPivot_SelectsRangeEitherWay()
	{
		_helper.HandleSingleSelect([], _deck[3]);

		Names(_helper.HandleAddRange([_deck[3]], _deck, _deck[1])).Should().Equal("c1", "c2", "c3");
		Names(_helper.HandleAddRange([], _deck, _deck[4])).Should().Equal("c3", "c4");
	}

	/// <summary>A card added with a control click becomes the pivot for the next shift click.</summary>
	[Fact]
	public void HandleAddRange_UsesPivotFromIndividualAdd()
	{
		_helper.HandleIndividualAddRemove([], _deck[4]);

		Names(_helper.HandleAddRange([], _deck, _deck[2])).Should().Equal("c2", "c3", "c4");
	}

	/// <summary>A shift click on a card that is not in the deck leaves the selection unchanged.</summary>
	[Fact]
	public void HandleAddRange_UnknownCard_LeavesSelection()
	{
		List<Card> selection = [_deck[0]];

		_helper.HandleAddRange(selection, _deck, new Card { Name = "stranger" }).Should().BeSameAs(selection);
	}

	/// <summary>If the pivot has left the deck, the shift click is ignored and the next one starts from the first card.</summary>
	[Fact]
	public void HandleAddRange_PivotGone_ResetsPivot()
	{
		var removed = _deck[1];
		_helper.HandleSingleSelect([], removed);
		_deck.Remove(removed);
		List<Card> selection = [removed];

		_helper.HandleAddRange(selection, _deck, _deck[2]).Should().BeSameAs(selection);
		Names(_helper.HandleAddRange([], _deck, _deck[1])).Should().Equal("c0", "c2");
	}

	/// <summary>A range that would include a missing card leaves the selection unchanged.</summary>
	[Fact]
	public void HandleAddRange_RangeContainsNull_LeavesSelection()
	{
		_deck[1] = null!;
		List<Card> selection = [];

		_helper.HandleAddRange(selection, _deck, _deck[3]).Should().BeSameAs(selection);
		selection.Should().BeEmpty();
	}

	private sealed class Card : ICard
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public int? DeckPosition { get; set; }

		public string Name { get; set; } = string.Empty;
	}
}
