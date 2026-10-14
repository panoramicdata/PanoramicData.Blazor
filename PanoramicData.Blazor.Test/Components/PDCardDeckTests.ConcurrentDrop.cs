using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that a drop arriving while cards are still migrating between decks leaves each card in one place.
/// </summary>
public partial class PDCardDeckTests
{
	private const string AnimationModulePath = "./_content/PanoramicData.Blazor/PDAnimation.razor.js";

	private IRenderedComponent<PDCardDeckGroup<Card>> RenderAnimatedGroup(Board board)
	{
		var group = Render<PDCardDeckGroup<Card>>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<Card>())
			.Add(p => p.Transformation, (_, source, destination, cards) => board.Transform(source, destination, cards))
			.AddChildContent<PDCardDeck<Card>>(deck => deck
				.Add(p => p.Id, "left")
				.Add(p => p.IsAnimated, true)
				.Add(p => p.DataFunction, board.DataFor("left")))
			.AddChildContent<PDCardDeck<Card>>(deck => deck
				.Add(p => p.Id, "right")
				.Add(p => p.IsAnimated, true)
				.Add(p => p.DataFunction, board.DataFor("right"))));
		group.WaitForAssertion(() => group.FindAll("div.card").Should().HaveCount(3));
		return group;
	}

	/// <summary>
	/// Verifies that the drop reaching every deck at once, as each deck listens for it on the document, transforms once.
	/// </summary>
	[Fact]
	public async Task Group_DropReachingEveryDeck_TransformsOnce()
	{
		var board = new Board { TransformDuration = TimeSpan.FromMilliseconds(100) };
		var group = RenderGroup(board);
		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragStartAsync(new DragEventArgs()));
		await group.InvokeAsync(() => group.Find("#right").DragEnterAsync(new DragEventArgs()));

		await group.InvokeAsync(() => Task.WhenAll(
			Deck(group, "left").Instance.InitiateTransformAsync(),
			Deck(group, "right").Instance.InitiateTransformAsync()));

		board.Transforms.Should().ContainSingle();
		group.WaitForAssertion(() => Names(group, "right").Should().Equal("A", "C"));
	}

	/// <summary>
	/// Verifies that a drop arriving while the cards are still leaving the source deck does not put them in the destination twice.
	/// </summary>
	[Fact]
	public async Task Group_DropDuringTheMigration_PutsTheCardInTheDestinationOnce()
	{
		var board = new Board { MovesTheDroppedCards = true };
		var cancelAnimation = JSInterop.SetupModule(AnimationModulePath).SetupVoid("cancelAnimation", _ => true);
		var group = RenderAnimatedGroup(board);
		await group.InvokeAsync(() => group.FindAll("#left div.card")[0].DragStartAsync(new DragEventArgs()));

		var entering = group.InvokeAsync(() => group.Find("#right").DragEnterAsync(new DragEventArgs()));
		var dropping = group.InvokeAsync(Deck(group, "right").Instance.InitiateTransformAsync);
		cancelAnimation.SetVoidResult();
		await Task.WhenAll(entering, dropping);

		group.WaitForAssertion(() => Names(group, "right").Should().Equal("A", "C"));
		Names(group, "left").Should().Equal("B");
	}
}
