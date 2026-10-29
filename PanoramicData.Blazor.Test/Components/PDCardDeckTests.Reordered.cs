using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests of <see cref="PDCardDeck{TCard}.CardsReordered"/>, raised by a deck outside a group when the user changes its order.
/// </summary>
public partial class PDCardDeckTests
{
	private IRenderedComponent<PDCardDeck<Card>> RenderReportingDeck(List<IReadOnlyList<Card>> reports)
		=> RenderDeck(more: p => p.Add(x => x.CardsReordered, cards => reports.Add(cards)));

	private static async Task DragAsync(IRenderedComponent<PDCardDeck<Card>> deck, string card, string over)
	{
		await deck.InvokeAsync(() => CardElement(deck, card).DragStartAsync(new DragEventArgs()));
		await deck.InvokeAsync(() => CardElement(deck, over).DragOverAsync(new DragEventArgs()));
	}

	/// <summary>
	/// Verifies that a drag that changes the order reports the new order once, with each card's position set,
	/// although both the drop and the end of the drag arrive.
	/// </summary>
	[Fact]
	public async Task DragThenDrop_ReportsTheNewOrderOnce()
	{
		List<IReadOnlyList<Card>> reports = [];
		var deck = RenderReportingDeck(reports);

		await DragAsync(deck, "Alpha", over: "Charlie");
		await deck.InvokeAsync(deck.Instance.InitiateTransformAsync);
		await deck.InvokeAsync(() => CardElement(deck, "Alpha").DragEndAsync(new DragEventArgs()));

		reports.Should().ContainSingle();
		reports[0].Select(c => c.Name).Should().Equal("Bravo", "Charlie", "Alpha");
		reports[0].Select(c => c.DeckPosition).Should().Equal(0, 1, 2);
	}

	/// <summary>
	/// Verifies that the order is reported once when the end of the drag arrives before the drop.
	/// </summary>
	[Fact]
	public async Task DragEndBeforeTheDrop_ReportsOnce()
	{
		List<IReadOnlyList<Card>> reports = [];
		var deck = RenderReportingDeck(reports);

		await DragAsync(deck, "Alpha", over: "Charlie");
		await deck.InvokeAsync(() => CardElement(deck, "Alpha").DragEndAsync(new DragEventArgs()));
		await deck.InvokeAsync(deck.Instance.InitiateTransformAsync);

		reports.Should().ContainSingle();
		reports[0].Select(c => c.Name).Should().Equal("Bravo", "Charlie", "Alpha");
	}

	/// <summary>
	/// Verifies that a drag which moves a card away and back again, ending in the order it began, reports nothing.
	/// </summary>
	[Fact]
	public async Task DragEndingWhereItBegan_ReportsNothing()
	{
		List<IReadOnlyList<Card>> reports = [];
		var deck = RenderReportingDeck(reports);

		await DragAsync(deck, "Alpha", over: "Bravo");
		Names(deck).Should().Equal("Bravo", "Alpha", "Charlie");
		await deck.InvokeAsync(() => CardElement(deck, "Bravo").DragOverAsync(new DragEventArgs()));
		Names(deck).Should().Equal("Alpha", "Bravo", "Charlie");
		await deck.InvokeAsync(deck.Instance.InitiateTransformAsync);
		await deck.InvokeAsync(() => CardElement(deck, "Alpha").DragEndAsync(new DragEventArgs()));

		reports.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the drop, which the script sends to every deck on the page, is reported only by the deck dragged in.
	/// </summary>
	[Fact]
	public async Task DropReachingEveryDeck_IsReportedOnlyByTheDraggedDeck()
	{
		List<IReadOnlyList<Card>> draggedReports = [];
		List<IReadOnlyList<Card>> otherReports = [];
		var dragged = RenderReportingDeck(draggedReports);
		var other = RenderReportingDeck(otherReports);

		await DragAsync(dragged, "Alpha", over: "Charlie");
		await other.InvokeAsync(other.Instance.InitiateTransformAsync);
		await dragged.InvokeAsync(dragged.Instance.InitiateTransformAsync);

		otherReports.Should().BeEmpty();
		draggedReports.Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that every drag that changes the order is reported, not only the first.
	/// </summary>
	[Fact]
	public async Task EachDrag_ReportsItsOwnOrder()
	{
		List<IReadOnlyList<Card>> reports = [];
		var deck = RenderReportingDeck(reports);

		await DragAsync(deck, "Alpha", over: "Charlie");
		await deck.InvokeAsync(deck.Instance.InitiateTransformAsync);
		await DragAsync(deck, "Charlie", over: "Bravo");
		await deck.InvokeAsync(deck.Instance.InitiateTransformAsync);

		reports.Select(r => r.Select(c => c.Name)).Should().SatisfyRespectively(
			first => first.Should().Equal("Bravo", "Charlie", "Alpha"),
			second => second.Should().Equal("Charlie", "Bravo", "Alpha"));
	}

	/// <summary>
	/// Verifies that a deck in a group leaves its reorder to the group's transformation and does not report it as well.
	/// </summary>
	[Fact]
	public async Task Group_ReorderInADeck_IsLeftToTheTransformation()
	{
		var board = new Board();
		List<IReadOnlyList<Card>> reports = [];
		var group = Render<PDCardDeckGroup<Card>>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<Card>())
			.Add(p => p.Transformation, (_, source, destination, cards) => board.Transform(source, destination, cards))
			.AddChildContent<PDCardDeck<Card>>(deck => deck
				.Add(p => p.Id, "left")
				.Add(p => p.DataFunction, board.DataFor("left"))
				.Add(p => p.CardsReordered, cards => reports.Add(cards))));
		group.WaitForAssertion(() => group.FindAll("div.card").Should().HaveCount(2));

		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragStartAsync(new DragEventArgs()));
		await group.InvokeAsync(() => group.Find("#left").DragEnterAsync(new DragEventArgs()));
		await group.InvokeAsync(() => group.FindAll("#left div.card")[1].DragOverAsync(new DragEventArgs()));
		await group.InvokeAsync(Deck(group, "left").Instance.InitiateTransformAsync);
		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragEndAsync(new DragEventArgs()));

		board.Transforms.Should().ContainSingle();
		reports.Should().BeEmpty();
	}
}
