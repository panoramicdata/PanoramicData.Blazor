using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// <see cref="PDStudio.OnExecutionStateChanged"/> reports the start of a run and its end, however it ends, once each.
/// </summary>
public partial class PDStudioTests
{
	private IRenderedComponent<PDStudio> RenderReportingStates(List<bool> states)
		=> RenderStudio(more: p => p.Add(x => x.OnExecutionStateChanged, s => states.Add(s)));

	/// <summary>
	/// Verifies that a run returning a result set, which raises no service events, still reports its end.
	/// </summary>
	[Fact]
	public async Task ExecutionState_AResultSetRun_ReportsItsEnd()
	{
		var states = new List<bool>();
		var studio = RenderReportingStates(states);
		_service.ResultSet = new PDStudioResultSet { Columns = [new PDStudioResultColumn("n", "integer")], Rows = [[1]] };

		await TypeAsync(studio, "SELECT 1");
		await PlayButton(studio).ClickAsync(new());

		studio.WaitForAssertion(() => states.Should().Equal(true, false));
	}

	/// <summary>
	/// Verifies that a cancelled run and a failed run each report their end.
	/// </summary>
	[Fact]
	public async Task ExecutionState_ACancelledOrFailedRun_ReportsItsEnd()
	{
		var states = new List<bool>();
		var studio = RenderReportingStates(states);
		_service.Block = true;
		await TypeAsync(studio, "sleep(10)");

		var running = PlayButton(studio).ClickAsync(new());
		studio.WaitForAssertion(() => states.Should().Equal(true));
		await PlayButton(studio).ClickAsync(new());
		await running;
		studio.WaitForAssertion(() => states.Should().Equal(true, false));

		_service.Block = false;
		_service.Failure = new InvalidOperationException("boom");
		await PlayButton(studio).ClickAsync(new());
		studio.WaitForAssertion(() => states.Should().Equal(true, false, true, false));
	}

	/// <summary>
	/// Verifies that a run the service also reports as completed is reported as ended only once.
	/// </summary>
	[Fact]
	public async Task ExecutionState_AnEndReportedByTheServiceToo_IsReportedOnce()
	{
		var states = new List<bool>();
		var studio = RenderReportingStates(states);
		_service.RaiseCompletedOnFinish = true;

		await TypeAsync(studio, "1 + 1");
		await PlayButton(studio).ClickAsync(new());
		studio.WaitForAssertion(() => Status(studio).Should().Be("Complete"));

		states.Should().Equal(true, false);
	}
}
