using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for the obsolete <see cref="DelegatedDataProvider{TItem}"/>, kept for backward compatibility.</summary>
#pragma warning disable CS0618 // The type under test is obsolete by design.
public class DelegatedDataProviderTests
{
	private static CancellationToken Token => TestContext.Current.CancellationToken;

	/// <summary>Each interface member forwards its arguments to the matching delegate and returns its result.</summary>
	[Fact]
	public async Task Members_ForwardToDelegates()
	{
		var calls = new List<string>();
		var provider = new DelegatedDataProvider<string>
		{
			CreateAsync = (item, _) => { calls.Add($"create {item}"); return Task.FromResult(new OperationResponse { Success = true }); },
			DeleteAsync = (item, _) => { calls.Add($"delete {item}"); return Task.FromResult(new OperationResponse { ErrorMessage = "d" }); },
			UpdateAsync = (item, d, _) => { calls.Add($"update {item} {d.Count}"); return Task.FromResult(new OperationResponse { ErrorMessage = "u" }); },
			GetDataAsync = (_, _) => Task.FromResult(new DataResponse<string>(["x", "y"], 2))
		};
		IDataProviderService<string> service = provider;

		(await service.CreateAsync("a", Token)).Success.Should().BeTrue();
		(await service.DeleteAsync("b", Token)).ErrorMessage.Should().Be("d");
		(await service.UpdateAsync("c", new Dictionary<string, object?> { ["k"] = 1 }, Token)).ErrorMessage.Should().Be("u");
		(await service.GetDataAsync(new DataRequest<string>(), Token)).TotalCount.Should().Be(2);
		calls.Should().Equal("create a", "delete b", "update c 1");
	}

	/// <summary>Each interface member throws when its delegate has not been supplied.</summary>
	[Fact]
	public async Task Members_WithoutDelegates_ThrowNotImplemented()
	{
		IDataProviderService<string> service = new DelegatedDataProvider<string>();

		await ((Func<Task>)(() => service.CreateAsync("a", Token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => service.DeleteAsync("a", Token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => service.UpdateAsync("a", new Dictionary<string, object?>(), Token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => service.GetDataAsync(new DataRequest<string>(), Token))).Should().ThrowAsync<NotImplementedException>();
	}
}
#pragma warning restore CS0618
