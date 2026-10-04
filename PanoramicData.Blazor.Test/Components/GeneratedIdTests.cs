using System.Collections.Concurrent;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that components which replace the generic <c>pd-component-N</c> id with their own default keep doing
/// so when another component is constructed between their construction and their initialisation, as happens
/// under concurrent rendering (issue #159), and that the shared sequence never hands out the same id twice.
/// </summary>
public partial class GeneratedIdTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public GeneratedIdTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/PanoramicData.Blazor/PDGraph.razor.js");
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>Verifies the dashboard keeps its own default id.</summary>
	[Fact]
	public void Dashboard_keeps_its_default_id_when_another_component_is_constructed_first()
		=> AssertDefaultId(Render<RacedDashboard>(), "pd-dashboard-");

	/// <summary>Verifies the widget keeps its own default id.</summary>
	[Fact]
	public void Widget_keeps_its_default_id_when_another_component_is_constructed_first()
		=> AssertDefaultId(Render<RacedWidget>(), "pd-widget-");

	/// <summary>Verifies the graph controls keep their own default id.</summary>
	[Fact]
	public void GraphControls_keep_their_default_id_when_another_component_is_constructed_first()
		=> AssertDefaultId(Render<RacedGraphControls>(), "pd-graph-controls-");

	/// <summary>Verifies the graph information panel keeps its own default id.</summary>
	[Fact]
	public void GraphInfo_keeps_its_default_id_when_another_component_is_constructed_first()
		=> AssertDefaultId(Render<RacedGraphInfo>(), "pd-graph-info-");

	/// <summary>Verifies the graph selection panel keeps its own default id.</summary>
	[Fact]
	public void GraphSelectionInfo_keeps_its_default_id_when_another_component_is_constructed_first()
		=> AssertDefaultId(Render<RacedGraphSelectionInfo>(), "pd-graph-selection-info-");

	/// <summary>Verifies the graph viewer keeps its own default id.</summary>
	[Fact]
	public void GraphViewer_keeps_its_default_id_when_another_component_is_constructed_first()
		=> AssertDefaultId(Render<RacedGraphViewer>(), "pd-graph-viewer-");

	/// <summary>
	/// Verifies that components constructed concurrently on many threads are all given distinct ids.
	/// </summary>
	[Fact]
	public void Components_constructed_concurrently_get_distinct_ids()
	{
		var ids = new ConcurrentBag<string>();

		Parallel.For(0, 20_000, _ => ids.Add(new PDComponentBase().Id));

		ids.Distinct().Should().HaveCount(20_000);
	}

	/// <summary>
	/// Verifies that graph information panels initialised concurrently are all given distinct ids of their own
	/// form, none of them falling back to the generic one.
	/// </summary>
	[Fact]
	public void Components_initialised_concurrently_get_distinct_ids_of_their_own_form()
	{
		var ids = new ConcurrentBag<string>();

		Parallel.For(0, 5_000, _ => ids.Add(InitialisableSelectionInfo.CreateAndInitialise()));

		ids.Should().OnlyContain(id => id.StartsWith("pd-graph-selection-info-", StringComparison.Ordinal));
		ids.Distinct().Should().HaveCount(5_000);
	}

	private static void AssertDefaultId<TComponent>(IRenderedComponent<TComponent> component, string prefix)
		where TComponent : PDComponentBase
	{
		component.Instance.Id.Should().MatchRegex($"^{prefix}[0-9]+$");
		component.Find($"#{component.Instance.Id}").Should().NotBeNull();
	}
}
