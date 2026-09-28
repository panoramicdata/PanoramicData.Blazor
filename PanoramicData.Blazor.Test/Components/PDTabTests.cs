using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the behaviour of a single <see cref="PDTab"/>: its identity, title synchronisation and content, both
/// inside a <see cref="PDTabSet"/> and on its own.
/// </summary>
public class PDTabTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDTabTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static RenderFragment OneTab(Guid? id, string title, string? icon = null) => builder =>
	{
		builder.OpenComponent<PDTab>(0);
		if (id is not null)
		{
			builder.AddComponentParameter(1, nameof(PDTab.Id), id.Value);
		}

		builder.AddComponentParameter(2, nameof(PDTab.Title), title);
		builder.AddComponentParameter(3, nameof(PDTab.IconCssClass), icon);
		builder.AddComponentParameter(4, nameof(PDTab.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, $"<p class=\"body\">{title} body</p>")));
		builder.CloseComponent();
	};

	/// <summary>
	/// Verifies that a supplied identifier is kept, and that the tab's title, icon and content are what its
	/// tab set renders.
	/// </summary>
	[Fact]
	public void Tab_KeepsSuppliedId_AndExposesTitleAndContent()
	{
		var id = Guid.NewGuid();
		var set = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, OneTab(id, "Alpha", "bi bi-star")));

		var tab = set.FindComponent<PDTab>().Instance;
		tab.Id.Should().Be(id);
		tab.GetTitle().Should().Be("Alpha");
		tab.GetChildContent().Should().NotBeNull();

		set.Find(".pdtabset-tab-title").TextContent.Should().Be("Alpha");
		set.Find("i.pdtabset-tab-icon").ClassList.Should().Contain(["bi", "bi-star"]);
		set.Find(".pdtabset-content p.body").TextContent.Should().Be("Alpha body");
	}

	/// <summary>
	/// Verifies that a tab with no identifier is given a fresh one.
	/// </summary>
	[Fact]
	public void Tab_WithoutId_IsGivenOne()
	{
		var set = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, OneTab(null, "Alpha")));

		set.FindComponent<PDTab>().Instance.Id.Should().NotBe(Guid.Empty);
		set.FindAll("i.pdtabset-tab-icon").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the rename buffer follows the title while the tab is not being renamed, and is left alone
	/// while it is.
	/// </summary>
	[Fact]
	public void TempTitle_FollowsTitle_OnlyWhenNotRenaming()
	{
		var tab = Render<PDTab>(parameters => parameters.Add(p => p.Title, "First"));
		tab.Instance.TempTitle.Should().Be("First");

		tab.Render(parameters => parameters.Add(p => p.Title, "Second"));
		tab.Instance.TempTitle.Should().Be("Second");

		tab.Instance.IsRenaming = true;
		tab.Instance.TempTitle = "Being typed";
		tab.Render(parameters => parameters.Add(p => p.Title, "Third"));
		tab.Instance.TempTitle.Should().Be("Being typed");
		tab.Instance.GetTitle().Should().Be("Third");
	}

	/// <summary>
	/// Verifies that a tab rendered outside any tab set renders nothing and can be disposed safely.
	/// </summary>
	[Fact]
	public void Tab_WithoutTabSet_RendersNothing_AndDisposesSafely()
	{
		var tab = Render<PDTab>(parameters => parameters.Add(p => p.Title, "Orphan"));

		tab.Markup.Trim().Should().BeEmpty();
		tab.Instance.GetChildContent().Should().BeNull();
		tab.Instance.Dispose();
		tab.Instance.GetTitle().Should().Be("Orphan");
	}
}
