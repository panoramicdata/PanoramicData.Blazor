using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Connector click and animation tests for <see cref="PDTiles"/>.
/// </summary>
public partial class PDTilesTests
{
	/// <summary>Clicking a straight or curved connector raises <see cref="PDTiles.ConnectorClick"/> with its name, ends and definition.</summary>
	[Theory]
	[InlineData(ConnectionMode.StraightLine)]
	[InlineData(ConnectionMode.RowCurves)]
	public async Task ConnectorClick_RaisesEventWithConnectorDetails(ConnectionMode mode)
	{
		var connector = Connector(0, 0, 1, 1, "down");
		ConnectorClickEventArgs? received = null;
		var cut = Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 2 })
			.Add(x => x.ConnectorOptions, StillConnectors(mode))
			.Add(x => x.Logos, ["tiles/Alpha Logo.svg"])
			.Add(x => x.Connectors, [connector])
			.Add(x => x.ConnectorClick, args => received = args));

		await cut.Find("g.connector").ClickAsync(new MouseEventArgs());

		received.Should().NotBeNull();
		received!.ConnectorName.Should().Be("Alpha?Alpha#0");
		received.Connector.Should().BeSameAs(connector);
		received.StartTile.Should().BeSameAs(connector.StartTile);
		received.EndTile.Should().BeSameAs(connector.EndTile);
	}

	/// <summary>
	/// How long to wait for the animation timer. The wait returns as soon as the condition holds, so this is only
	/// reached when something is wrong; it is long because the timer's ticks share the thread pool and the
	/// renderer's dispatcher with the whole suite, and under full-suite load with coverage the first tick and
	/// the check that observes it were seen to take longer than bUnit's one-second default.
	/// </summary>
	private static readonly TimeSpan _timerWait = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Renders the cheapest grid that can hold a connector, with animation on. Every tick of the 60 fps timer
	/// re-renders the whole component on the dispatcher, so a cheap render keeps those ticks from queuing up
	/// ahead of the test's own checks and parameter changes.
	/// </summary>
	private IRenderedComponent<PDTiles> RenderAnimated()
		=> Render<PDTiles>(p => p
			.Add(x => x.Options, new TileGridOptions { Columns = 2, Rows = 1, LineOpacity = 0, Glow = 0, Reflection = 0 })
			.Add(x => x.ConnectorOptions, new TileConnectorOptions { AnimationSpeed = 100 })
			.Add(x => x.Logos, [string.Empty])
			.Add(x => x.Connectors, [Connector(0, 0, 1, 0, "right")]));

	/// <summary>
	/// With connectors and a non-zero speed the pattern animates; removing the connectors stops it, and the
	/// component disposes cleanly.
	/// </summary>
	[Fact]
	public async Task Animation_RunsWithConnectors_AndStopsWithoutThem()
	{
		var cut = RenderAnimated();

		cut.WaitForAssertion(() => cut.Instance.AnimationOffset.Should().BeGreaterThan(0), _timerWait);

		cut.Render(p => p.Add(x => x.Connectors, null));
		cut.WaitForAssertion(() => cut.FindAll("g.connector").Should().BeEmpty(), _timerWait);
		await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
	}

	/// <summary>Turning the animation speed to zero while connectors are shown stops the animation but keeps the connector.</summary>
	[Fact]
	public void Animation_SpeedSetToZero_KeepsTheConnector()
	{
		var cut = RenderAnimated();
		cut.WaitForAssertion(() => cut.Instance.AnimationOffset.Should().BeGreaterThan(0), _timerWait);

		cut.Render(p => p.Add(x => x.ConnectorOptions, StillConnectors()));

		cut.WaitForAssertion(() => cut.Instance.ConnectorOptions.AnimationSpeed.Should().Be(0), _timerWait);
		cut.FindAll("g.connector").Should().ContainSingle();
	}
}
