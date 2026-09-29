using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Loading tests for <see cref="PDGraph{TItem}"/>: the spinner, and data providers that complete
/// asynchronously (issue #187).
/// </summary>
public partial class PDGraphTests
{
	/// <summary>
	/// Verifies that a graph with no data provider does not show the loading spinner forever.
	/// </summary>
	[Fact]
	public void Without_a_data_provider_the_spinner_is_not_shown()
	{
		var component = Render<PDGraph<GraphNode>>(parameters => parameters.Add(p => p.Id, GraphId));

		component.FindAll(".pd-graph-loading").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a data provider that completes asynchronously loads the graph, rather than failing
	/// to re-render off the renderer's dispatcher and leaving the spinner up.
	/// </summary>
	[Fact]
	public void An_asynchronous_data_provider_loads_the_graph()
	{
		var component = Render<PDGraph<GraphNode>>(parameters => parameters
			.Add(p => p.Id, GraphId)
			.Add(p => p.DataProvider, new DelayedGraphProvider(CreateData())));

		component.WaitForAssertion(
			() =>
			{
				component.FindAll(".pd-graph-loading").Should().BeEmpty();
				component.FindAll(".pd-graph-error").Should().BeEmpty();
				component.FindAll("svg").Should().NotBeEmpty();
			},
			TimeSpan.FromSeconds(10));
	}

	/// <summary>
	/// Verifies that awaiting a refresh from an asynchronous data provider completes without throwing.
	/// </summary>
	[Fact]
	public async Task Refreshing_from_an_asynchronous_data_provider_completes()
	{
		var provider = new DelayedGraphProvider(CreateData());
		var component = Render<PDGraph<GraphNode>>(parameters => parameters
			.Add(p => p.Id, GraphId)
			.Add(p => p.DataProvider, provider));
		component.WaitForState(() => provider.Requests > 0, TimeSpan.FromSeconds(10));

		await component.InvokeAsync(() => component.Instance.RefreshAsync(Xunit.TestContext.Current.CancellationToken));

		provider.Requests.Should().BeGreaterThan(1);
		component.FindAll(".pd-graph-loading").Should().BeEmpty();
	}

	/// <summary>
	/// Supplies one graph after yielding to a timer, so its continuation runs off the renderer's dispatcher
	/// unless the caller resumes on its captured context.
	/// </summary>
	/// <param name="data">The graph to supply.</param>
	private sealed class DelayedGraphProvider(GraphData data) : DataProviderBase<GraphData>
	{
		private int _requests;

		/// <summary>Gets the number of requests made.</summary>
		public int Requests => Volatile.Read(ref _requests);

		/// <inheritdoc />
		public override async Task<DataResponse<GraphData>> GetDataAsync(DataRequest<GraphData> request, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(request);
			Interlocked.Increment(ref _requests);
			await Task.Delay(20, cancellationToken).ConfigureAwait(false);
			return new DataResponse<GraphData>([data], 1);
		}
	}
}
