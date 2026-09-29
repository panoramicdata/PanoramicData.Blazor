using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="CardDeckDataProviderService{TItem}"/>.</summary>
public class CardDeckDataProviderServiceTests
{
	private static CancellationToken Token => TestContext.Current.CancellationToken;

	private static async Task<CardDeckDataProviderService<Card>> ServiceWithAsync(params Card[] cards)
	{
		var service = new CardDeckDataProviderService<Card>();
		foreach (var card in cards)
		{
			await service.CreateAsync(card, Token);
		}

		return service;
	}

	/// <summary>Create appends the card and reports success.</summary>
	[Fact]
	public async Task CreateAsync_AppendsCard()
	{
		var service = new CardDeckDataProviderService<Card>();
		service.List.Should().BeEmpty();

		var response = await service.CreateAsync(new Card { Title = "a" }, Token);

		response.Success.Should().BeTrue();
		service.List.Should().ContainSingle().Which.Title.Should().Be("a");
	}

	/// <summary>Getting data without a filter returns every card.</summary>
	[Fact]
	public async Task GetDataAsync_NoFilter_ReturnsAll()
	{
		var service = await ServiceWithAsync(new Card { Title = "a" }, new Card { Title = "b" });

		var response = await service.GetDataAsync(new DataRequest<Card>(), Token);

		response.Items.Select(c => c.Title).Should().Equal("a", "b");
		response.TotalCount.Should().Be(2);
	}

	/// <summary>A response filter narrows the items returned.</summary>
	[Fact]
	public async Task GetDataAsync_WithFilter_AppliesIt()
	{
		var service = await ServiceWithAsync(new Card { Title = "a" }, new Card { Title = "b" });
		var request = new DataRequest<Card> { ResponseFilter = cards => cards.Where(c => c.Title == "b") };

		var response = await service.GetDataAsync(request, Token);

		response.Items.Should().ContainSingle().Which.Title.Should().Be("b");
	}

	/// <summary>The delayed overload waits and then returns the same data.</summary>
	[Fact]
	public async Task GetDataAsync_WithDelay_ReturnsData()
	{
		var service = await ServiceWithAsync(new Card { Title = "a" });

		var response = await service.GetDataAsync(new DataRequest<Card>(), 0, Token);

		response.Items.Should().ContainSingle();
	}

	/// <summary>The delayed overload honours cancellation.</summary>
	[Fact]
	public async Task GetDataAsync_WithDelay_Cancelled_Throws()
	{
		var service = new CardDeckDataProviderService<Card>();
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		var act = () => service.GetDataAsync(new DataRequest<Card>(), 10, cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	/// <summary>Delete removes a present card and reports a missing one as not found.</summary>
	[Fact]
	public async Task DeleteAsync_RemovesOrReportsNotFound()
	{
		var card = new Card();
		var service = await ServiceWithAsync(card);

		(await service.DeleteAsync(card, Token)).Success.Should().BeTrue();
		(await service.DeleteAsync(card, Token)).ErrorMessage.Should().Be("Not found");
	}

	/// <summary>Update applies the delta, and reports missing cards, unknown properties and read-only properties.</summary>
	[Fact]
	public async Task UpdateAsync_AppliesDeltaOrReportsProblem()
	{
		var card = new Card { Title = "old" };
		var service = await ServiceWithAsync(card);

		(await service.UpdateAsync(card, new Dictionary<string, object?> { ["Title"] = "new" }, Token)).Success.Should().BeTrue();
		card.Title.Should().Be("new");

		(await service.UpdateAsync(new Card(), new Dictionary<string, object?>(), Token)).ErrorMessage.Should().Be("Not found");
		(await service.UpdateAsync(card, new Dictionary<string, object?> { ["Nope"] = 1 }, Token)).ErrorMessage.Should().Be("Property Nope not found");
		(await service.UpdateAsync(card, new Dictionary<string, object?> { ["Length"] = 1 }, Token)).ErrorMessage.Should().Be("Property Length can not be written too");
	}

	/// <summary>A value of the wrong type is reported rather than thrown.</summary>
	[Fact]
	public async Task UpdateAsync_WrongValueType_ReportsError()
	{
		var card = new Card();
		var service = await ServiceWithAsync(card);

		var response = await service.UpdateAsync(card, new Dictionary<string, object?> { ["Title"] = 5 }, Token);

		response.Success.Should().BeFalse();
		response.ErrorMessage.Should().NotBeEmpty();
	}

	private sealed class Card
	{
		public string Title { get; set; } = string.Empty;

		public int Length => Title.Length;
	}
}
