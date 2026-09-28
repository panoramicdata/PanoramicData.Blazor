using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="PanoramicData.Blazor.Extensions.NavigationManagerExtensions"/>, using bUnit's navigation manager.</summary>
public class NavigationManagerExtensionsTests : BunitContext
{
	private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

	/// <summary>Setting string values updates the query string of the current page.</summary>
	[Fact]
	public void SetUri_StringValues_UpdatesQuery()
	{
		Navigation.NavigateTo("/items?page=1&sort=name");

		Navigation.SetUri(new Dictionary<string, StringValues> { ["page"] = "2" });

		Navigation.Uri.Should().Be($"{Navigation.BaseUri}items?page=2&sort=name");
	}

	/// <summary>Setting object values writes each value's text form into the query string.</summary>
	[Fact]
	public void SetUri_ObjectValues_UpdatesQuery()
	{
		Navigation.NavigateTo("/items");

		Navigation.SetUri(new Dictionary<string, object> { ["page"] = 5, ["active"] = true });

		Navigation.Uri.Should().Be($"{Navigation.BaseUri}items?page=5&active=True");
	}

	/// <summary>The navigation replaces the current history entry rather than adding one.</summary>
	[Fact]
	public void SetUri_ReplacesHistoryEntry()
	{
		var navigation = Services.GetRequiredService<BunitNavigationManager>();
		navigation.NavigateTo("/items");

		navigation.SetUri(new Dictionary<string, StringValues> { ["q"] = "x" });

		navigation.History.First().Options.ReplaceHistoryEntry.Should().BeTrue();
	}
}
