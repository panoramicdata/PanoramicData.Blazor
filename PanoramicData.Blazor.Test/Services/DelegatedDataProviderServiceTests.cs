using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="DelegatedDataProviderService{TItem}"/>.</summary>
public class DelegatedDataProviderServiceTests
{
	private static CancellationToken Token => TestContext.Current.CancellationToken;

	/// <summary>Each interface member forwards its arguments to the matching delegate and returns its result.</summary>
	[Fact]
	public async Task Members_ForwardToDelegates()
	{
		var calls = new List<string>();
		var request = new DataRequest<string>();
		var delta = new Dictionary<string, object?>();
		var service = new DelegatedDataProviderService<string>
		{
			CreateAsync = (item, _) => { calls.Add($"create {item}"); return Task.FromResult(new OperationResponse { Success = true }); },
			DeleteAsync = (item, _) => { calls.Add($"delete {item}"); return Task.FromResult(new OperationResponse { ErrorMessage = "d" }); },
			UpdateAsync = (item, d, _) => { calls.Add($"update {item} {d.Count}"); return Task.FromResult(new OperationResponse { ErrorMessage = "u" }); },
			GetDataAsync = (r, _) => { r.Should().BeSameAs(request); return Task.FromResult(new DataResponse<string>(["x"], 1)); }
		};
		IDataProviderService<string> provider = service;

		(await provider.CreateAsync("a", Token)).Success.Should().BeTrue();
		(await provider.DeleteAsync("b", Token)).ErrorMessage.Should().Be("d");
		(await provider.UpdateAsync("c", delta, Token)).ErrorMessage.Should().Be("u");
		(await provider.GetDataAsync(request, Token)).Items.Should().Equal("x");
		calls.Should().Equal("create a", "delete b", "update c 0");
	}

	/// <summary>Each interface member throws when its delegate has not been supplied.</summary>
	[Fact]
	public async Task Members_WithoutDelegates_ThrowNotImplemented()
	{
		IDataProviderService<string> provider = new DelegatedDataProviderService<string>();

		await ((Func<Task>)(() => provider.CreateAsync("a", Token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => provider.DeleteAsync("a", Token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => provider.UpdateAsync("a", new Dictionary<string, object?>(), Token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => provider.GetDataAsync(new DataRequest<string>(), Token))).Should().ThrowAsync<NotImplementedException>();
	}
}
