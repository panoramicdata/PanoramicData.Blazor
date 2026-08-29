using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTabSet"/> keeps up with tabs rendered from a collection that changes
/// (issue #112, MS-25788).
/// </summary>
/// <remarks>
/// <see cref="PDTab"/> registers itself with its parent in <c>OnInitialized</c>. Until this work there was no
/// matching removal, which is invisible for the declarative case these components were written for - a fixed
/// set of tabs written out in markup, none of which ever leaves - and wrong the moment tabs come from a
/// collection: removing an item disposed its <see cref="PDTab"/> while the tab set went on rendering a tab
/// for it. The conversation tabs are exactly that case, which is why it surfaced here.
/// </remarks>
public class PDTabSetDynamicTabsTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDTabSetDynamicTabsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that removing an item from the collection behind the tabs removes its tab.
	/// </summary>
	/// <remarks>
	/// The regression this guards is a closed tab that stays in the strip: it looks live, and clicking it
	/// shows content belonging to an item that no longer exists.
	/// </remarks>
	[Fact]
	public void Removing_an_item_removes_its_tab()
	{
		var titles = new List<string> { "First", "Second", "Third" };

		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, TabsFor(titles)));

		component.FindAll(".pdtabset-tab-title").Should().HaveCount(3);

		titles.Remove("Second");
		component.Render();

		component.FindAll(".pdtabset-tab-title").Select(tab => tab.TextContent)
			.Should().Equal("First", "Third");
	}

	/// <summary>
	/// Verifies that removing the tab that was active leaves a valid selection rather than none.
	/// </summary>
	/// <remarks>
	/// Without this the tab set holds a reference to a disposed tab as its active one, and renders an empty
	/// content area beside a strip that plainly has tabs in it.
	/// </remarks>
	[Fact]
	public void Removing_the_active_tab_selects_another()
	{
		var titles = new List<string> { "First", "Second" };

		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, TabsFor(titles)));

		// The first tab is the one the set selects on its own as tabs register themselves.
		component.Find(".pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("First");

		titles.Remove("First");
		component.Render();

		component.FindAll(".pdtabset-tab-title").Should().ContainSingle();
		component.Find(".pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Second");
	}

	/// <summary>
	/// Verifies that adding an item adds its tab without disturbing the tabs already there.
	/// </summary>
	[Fact]
	public void Adding_an_item_adds_its_tab()
	{
		var titles = new List<string> { "First" };

		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, TabsFor(titles)));

		titles.Add("Second");
		component.Render();

		component.FindAll(".pdtabset-tab-title").Select(tab => tab.TextContent)
			.Should().Equal("First", "Second");
	}

	/// <summary>
	/// Renders one <see cref="PDTab"/> per title, keyed by title so that Blazor disposes the tab whose item
	/// was removed rather than re-labelling the survivors.
	/// </summary>
	private static RenderFragment TabsFor(List<string> titles) => builder =>
	{
		foreach (var title in titles)
		{
			builder.OpenComponent<PDTab>(0);
			builder.SetKey(title);
			builder.AddComponentParameter(1, nameof(PDTab.Title), title);
			builder.CloseComponent();
		}
	};
}
