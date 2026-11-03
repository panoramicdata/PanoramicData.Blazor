using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Result row clicks: a host can react to a click or Enter on a result row, and an unbound grid is unchanged.
/// </summary>
public partial class PDStudioTests
{
	private readonly List<PDStudioResultRowClickEventArgs> _rowClicks = [];

	private async Task<IRenderedComponent<PDStudio>> RunDevicesWithRowClickAsync()
	{
		_service.ResultSet = Devices();
		var studio = RenderStudio(more: p => p.Add(x => x.OnResultRowClick, (PDStudioResultRowClickEventArgs e) => _rowClicks.Add(e)));
		await TypeAsync(studio, "SELECT * FROM devices");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());
		studio.WaitForAssertion(() => studio.FindAll(".pd-studio-grid").Should().ContainSingle());
		return studio;
	}

	private static List<AngleSharp.Dom.IElement> BodyRows(IRenderedComponent<PDStudio> studio)
		=> [.. studio.FindAll(".pd-studio-grid tbody tr")];

	/// <summary>
	/// Verifies that with no row click bound the rows carry no attributes and no click handler, as before.
	/// </summary>
	[Fact]
	public async Task ResultRowClick_NotBound_RowsAreUnchanged()
	{
		var studio = await RunDevicesAsync();

		// The scoped CSS attribute (b-xxxx) is the only one a row has always carried.
		BodyRows(studio).Should().HaveCount(3)
			.And.OnlyContain(r => r.Attributes.Length == 1 && r.Attributes.All(a => a.Name.StartsWith("b-", StringComparison.Ordinal)));
		var click = () => BodyRows(studio)[0].Click();
		click.Should().Throw<MissingEventHandlerException>();
	}

	/// <summary>
	/// Verifies that clicking a row raises its index, values and the result set's columns, and that bound rows
	/// are marked clickable and focusable.
	/// </summary>
	[Fact]
	public async Task ResultRowClick_Click_RaisesTheRow()
	{
		var studio = await RunDevicesWithRowClickAsync();

		BodyRows(studio).Should().OnlyContain(r => r.ClassList.Contains("pd-studio-grid-clickable") && r.GetAttribute("tabindex") == "0");

		BodyRows(studio)[1].Click();

		var click = _rowClicks.Should().ContainSingle().Subject;
		click.RowIndex.Should().Be(1);
		click.Values.Should().Equal("WH2-AP-07", 3, null);
		click.Columns.Should().BeSameAs(_service.ResultSet!.Columns);
		click.GetValue("device").Should().Be("WH2-AP-07");
		click.GetValue("CLIENTS").Should().Be(3);
	}

	/// <summary>
	/// Verifies that after sorting, the raised index is the row's position in the result set, not on screen.
	/// </summary>
	[Fact]
	public async Task ResultRowClick_AfterSort_RaisesTheResultSetIndex()
	{
		var studio = await RunDevicesWithRowClickAsync();
		studio.FindAll(".pd-studio-grid-heading")[1].Click();

		BodyRows(studio)[0].Click();

		var click = _rowClicks.Should().ContainSingle().Subject;
		click.RowIndex.Should().Be(1);
		click.GetValue("device").Should().Be("WH2-AP-07");
	}

	/// <summary>
	/// Verifies that Enter on a focused row raises the event and other keys do not.
	/// </summary>
	[Fact]
	public async Task ResultRowClick_EnterKey_RaisesTheRow()
	{
		var studio = await RunDevicesWithRowClickAsync();

		BodyRows(studio)[2].KeyDown(new KeyboardEventArgs { Key = "a" });
		_rowClicks.Should().BeEmpty();

		BodyRows(studio)[2].KeyDown(new KeyboardEventArgs { Key = "Enter" });

		_rowClicks.Should().ContainSingle().Which.GetValue("device").Should().Be("BN-MX-01");
	}

	/// <summary>
	/// Verifies that asking for a column the result set does not have is an error rather than a NULL.
	/// </summary>
	[Fact]
	public void ResultRowClickEventArgs_UnknownColumn_Throws()
	{
		var set = Devices();
		var args = new PDStudioResultRowClickEventArgs(0, set.Rows[0], set.Columns);

		var read = () => args.GetValue("missing");

		read.Should().Throw<ArgumentException>().WithMessage("*missing*");
	}
}
