using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDCardDeckGroup{TCard}"/> moves cards between its decks by drag and drop.
/// </summary>
public partial class PDCardDeckGroupTests
{
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
	/// Verifies that when a second drag starts before the first has ended, only the two most recent decks count:
	/// entering a deck then moves the newer drag's cards, not the abandoned one's.
	/// </summary>
	[Fact]
	public async Task ARestartedDrag_MovesTheNewerDragsCards()
	{
		var group = RenderGroup("todo", "done");
		await StartDragAsync(group, "todo", "One");
		await StartDragAsync(group, "done", "Four");

		await group.InvokeAsync(() => Deck(group, "todo").RegisterDestination());

		Deck(group, "todo").Cards.Select(c => c.Name).Should().Contain("Four").And.Contain("One");
		Deck(group, "done").Cards.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a deck which is already part of the drag in progress is not registered with the group again.
	/// </summary>
	[Fact]
	public async Task ADeckAlreadyInTheDrag_IsNotRegisteredAgain()
	{
		var group = RenderGroup("todo", "done");
		await StartDragAsync(group, "todo", "One");

		await group.InvokeAsync(() => group.Instance.RegisterDeckAsChild(Deck(group, "todo")));

		group.Instance.Decks.Select(d => d.Id).Should().Equal("todo", "done");
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
		var (provider, source, destination, cards) = call.GetValueOrDefault();
		provider.Should().BeSameAs(_provider);
		(source, destination).Should().Be(("todo", "todo"));
		cards.Should().Equal("One");
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
}
