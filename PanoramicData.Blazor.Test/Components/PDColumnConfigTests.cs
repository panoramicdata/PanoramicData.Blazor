using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Services;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDColumnConfig"/>: the runtime overrides a table applies to its columns.
/// </summary>
public class PDColumnConfigTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDColumnConfigTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>
	/// Verifies that a new configuration targets no column and overrides nothing until told to.
	/// </summary>
	[Fact]
	public void NewConfig_OverridesNothing()
	{
		var config = new PDColumnConfig();

		config.Id.Should().BeEmpty();
		config.Title.Should().BeNull();
		config.Editable.Should().BeNull();
	}

	/// <summary>
	/// Verifies that a table given a configuration shows only the columns the configuration names.
	/// </summary>
	[Fact]
	public void Table_ShowsOnlyTheConfiguredColumns()
	{
		var provider = new ListDataProviderService<Row>([new Row("1", "Alpha", 3)]);

		var table = Render<PDTable<Row>>(parameters => parameters
			.Add(p => p.DataProvider, provider)
			.Add(p => p.KeyField, row => row.Id)
			.Add(p => p.ShowPager, false)
			.Add(p => p.ColumnsConfig, [new PDColumnConfig { Id = "Name" }])
			.AddChildContent<PDColumn<Row>>(column => column
				.Add(c => c.Id, "Name")
				.Add(c => c.Field, (Expression<Func<Row, object>>)(row => row.Name)))
			.AddChildContent<PDColumn<Row>>(column => column
				.Add(c => c.Id, "Count")
				.Add(c => c.Field, (Expression<Func<Row, object>>)(row => row.Count))));

		table.WaitForAssertion(() => table.FindAll("thead th").Select(th => th.TextContent.Trim())
			.Should().Equal("Name"));
	}

	private sealed record Row(string Id, string Name, int Count);
}
