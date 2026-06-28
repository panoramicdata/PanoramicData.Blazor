using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

public class ColumnGroupHelperTests
{
	#region IsInActiveGroup

	[Fact]
	public void IsInActiveGroup_NoActiveGroup_ShowsEverything()
	{
		ColumnGroupHelper.IsInActiveGroup("Stats", null).ShouldBeTrue();
		ColumnGroupHelper.IsInActiveGroup("Stats", "").ShouldBeTrue();
		ColumnGroupHelper.IsInActiveGroup(null, null).ShouldBeTrue();
	}

	[Fact]
	public void IsInActiveGroup_UngroupedColumn_AlwaysShown()
	{
		ColumnGroupHelper.IsInActiveGroup(null, "Stats").ShouldBeTrue();
		ColumnGroupHelper.IsInActiveGroup("", "Stats").ShouldBeTrue();
	}

	[Fact]
	public void IsInActiveGroup_GroupedColumn_ShownOnlyWhenMatchingActiveGroup()
	{
		ColumnGroupHelper.IsInActiveGroup("Stats", "Stats").ShouldBeTrue();
		ColumnGroupHelper.IsInActiveGroup("Stats", "Identity").ShouldBeFalse();
	}

	[Fact]
	public void IsInActiveGroup_IsCaseSensitive()
		=> ColumnGroupHelper.IsInActiveGroup("Stats", "stats").ShouldBeFalse();

	#endregion

	#region BuildPills

	[Fact]
	public void BuildPills_OrdersRegisteredGroupsByOrdinalThenRegistration()
	{
		var registered = new List<ColumnGroupContext>
		{
			new() { Name = "Dates", Ordinal = 40 },
			new() { Name = "Identity", Ordinal = 10 },
			new() { Name = "Contact", Ordinal = 20 },
		};
		var columnGroups = new string?[] { "Identity", "Identity", "Contact", "Dates" };

		var pills = ColumnGroupHelper.BuildPills(registered, columnGroups);

		pills.Select(p => p.Name).ShouldBe(["Identity", "Contact", "Dates"]);
	}

	[Fact]
	public void BuildPills_CountsListableColumnsPerGroup()
	{
		var registered = new List<ColumnGroupContext> { new() { Name = "Identity", Ordinal = 10 } };
		var columnGroups = new string?[] { "Identity", "Identity", "Identity", null, "" };

		var pills = ColumnGroupHelper.BuildPills(registered, columnGroups);

		pills.Single(p => p.Name == "Identity").Count.ShouldBe(3);
	}

	[Fact]
	public void BuildPills_IncludesStringOnlyGroupsAfterRegistered_InFirstSeenOrder()
	{
		var registered = new List<ColumnGroupContext> { new() { Name = "Identity", Ordinal = 10 } };
		var columnGroups = new string?[] { "Identity", "Zeta", "Alpha" };

		var pills = ColumnGroupHelper.BuildPills(registered, columnGroups);

		pills.Select(p => p.Name).ShouldBe(["Identity", "Zeta", "Alpha"]);
	}

	[Fact]
	public void BuildPills_IgnoresUngroupedColumns()
		=> ColumnGroupHelper.BuildPills([], [null, "", null]).ShouldBeEmpty();

	[Fact]
	public void BuildPills_CarriesIconAndDescriptionFromRegisteredGroup()
	{
		var registered = new List<ColumnGroupContext>
		{
			new() { Name = "Stats", Icon = "fas fa-chart-bar", Description = "Metrics", Ordinal = 10 }
		};

		var pill = ColumnGroupHelper.BuildPills(registered, ["Stats"]).Single();

		pill.Icon.ShouldBe("fas fa-chart-bar");
		pill.Description.ShouldBe("Metrics");
	}

	[Fact]
	public void BuildPills_DeduplicatesRegisteredGroupsByName()
	{
		var registered = new List<ColumnGroupContext>
		{
			new() { Name = "Identity", Ordinal = 10 },
			new() { Name = "Identity", Ordinal = 99 },
		};

		ColumnGroupHelper.BuildPills(registered, ["Identity"]).Count.ShouldBe(1);
	}

	#endregion
}
