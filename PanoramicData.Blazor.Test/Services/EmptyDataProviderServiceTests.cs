using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="EmptyDataProviderService{TItem}"/>.</summary>
public class EmptyDataProviderServiceTests
{
	/// <summary>Getting data always returns no items and a total of zero.</summary>
	[Fact]
	public async Task GetDataAsync_ReturnsNothing()
	{
		var service = new EmptyDataProviderService<string>();

		var response = await service.GetDataAsync(new DataRequest<string>(), TestContext.Current.CancellationToken);

		response.Items.Should().BeEmpty();
		response.TotalCount.Should().Be(0);
	}

	/// <summary>Create, update and delete all report success without doing anything.</summary>
	[Fact]
	public async Task Mutations_ReportSuccess()
	{
		var service = new EmptyDataProviderService<string>();
		var token = TestContext.Current.CancellationToken;

		(await service.CreateAsync("a", token)).Success.Should().BeTrue();
		(await service.UpdateAsync("a", new Dictionary<string, object?>(), token)).Success.Should().BeTrue();
		(await service.DeleteAsync("a", token)).Success.Should().BeTrue();
		(await service.GetDataAsync(new DataRequest<string>(), token)).Items.Should().BeEmpty();
	}
}
