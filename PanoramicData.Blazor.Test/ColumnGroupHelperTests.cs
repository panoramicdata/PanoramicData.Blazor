using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the ColumnGroupHelper class.</summary>
public class ColumnGroupHelperTests
{
	#region IsInActiveGroup

	/// <summary>Verifies that when there is no active group, all columns are shown regardless of their group assignment.</summary>
	[Fact]
	public void IsInActiveGroup_NoActiveGroup_ShowsEverything()
	{
		ColumnGroupHelper.IsInActiveGroup("Stats", null).ShouldBeTrue();
		ColumnGroupHelper.IsInActiveGroup("Stats", "").ShouldBeTrue();
		ColumnGroupHelper.IsInActiveGroup(null, null).ShouldBeTrue();
	}

	/// <summary>Verifies that ungrouped columns are always shown even when a specific group is active.</summary>
	[Fact]
	public void IsInActiveGroup_UngroupedColumn_AlwaysShown()
	{
		ColumnGroupHelper.IsInActiveGroup(null, "Stats").ShouldBeTrue();
		ColumnGroupHelper.IsInActiveGroup("", "Stats").ShouldBeTrue();
	}

	/// <summary>Verifies that a grouped column is shown only when its group name matches the active group.</summary>
	[Fact]
	public void IsInActiveGroup_GroupedColumn_ShownOnlyWhenMatchingActiveGroup()
	{
		ColumnGroupHelper.IsInActiveGroup("Stats", "Stats").ShouldBeTrue();
		ColumnGroupHelper.IsInActiveGroup("Stats", "Identity").ShouldBeFalse();
	}

	/// <summary>Verifies that group name comparison is case-sensitive.</summary>
	[Fact]
	public void IsInActiveGroup_IsCaseSensitive()
		=> ColumnGroupHelper.IsInActiveGroup("Stats", "stats").ShouldBeFalse();

	#endregion

	#region BuildPills

	/// <summary>Verifies that BuildPills returns groups ordered by their registered ordinal value.</summary>
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

	/// <summary>Verifies that BuildPills counts the number of listable columns belonging to each group.</summary>
	[Fact]
	public void BuildPills_CountsListableColumnsPerGroup()
	{
		var registered = new List<ColumnGroupContext> { new() { Name = "Identity", Ordinal = 10 } };
		var columnGroups = new string?[] { "Identity", "Identity", "Identity", null, "" };

		var pills = ColumnGroupHelper.BuildPills(registered, columnGroups);

		pills.Single(p => p.Name == "Identity").Count.ShouldBe(3);
	}

	/// <summary>Verifies that groups not in the registered list are appended after registered groups in first-seen order.</summary>
	[Fact]
	public void BuildPills_IncludesStringOnlyGroupsAfterRegistered_InFirstSeenOrder()
	{
		var registered = new List<ColumnGroupContext> { new() { Name = "Identity", Ordinal = 10 } };
		var columnGroups = new string?[] { "Identity", "Zeta", "Alpha" };

		var pills = ColumnGroupHelper.BuildPills(registered, columnGroups);

		pills.Select(p => p.Name).ShouldBe(["Identity", "Zeta", "Alpha"]);
	}

	/// <summary>Verifies that null and empty group strings are excluded from the built pills.</summary>
	[Fact]
	public void BuildPills_IgnoresUngroupedColumns()
		=> ColumnGroupHelper.BuildPills([], [null, "", null]).ShouldBeEmpty();

	/// <summary>Verifies that the icon and description from the registered group are preserved in the resulting pill.</summary>
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

	/// <summary>Verifies that duplicate registrations of the same group name produce only a single pill.</summary>
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
