using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTabSet"/> selects, closes, adds, renames and reorders tabs, and raises the matching
/// callbacks.
/// </summary>
/// <remarks>Dynamic add and remove of tabs from a collection is covered by <see cref="PDTabSetDynamicTabsTests"/>.</remarks>
public partial class PDTabSetTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDTabSetTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Tabs render their title, icon and CSS, the widths are applied and the first tab's content shows.</summary>
	[Fact]
	public void Tabs_render_with_icon_css_widths_and_first_content()
	{
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.CssClass, "outer")
			.Add(p => p.TabMinWidth, "50px")
			.Add(p => p.TabMaxWidth, "90px")
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Icon = "fas fa-1", Css = "first" }, new Spec("Two"))));

		component.Find(".pdtabset").ClassList.Should().Contain("outer");
		var buttons = component.FindAll("button.pdtabset-tab");
		buttons[0].ClassList.Should().Contain("active").And.Contain("first");
		buttons[0].QuerySelector("i.pdtabset-tab-icon")!.ClassList.Should().Contain("fa-1");
		buttons[1].QuerySelector("i").Should().BeNull();
		buttons[0].GetAttribute("style").Should().Contain("min-width:50px").And.Contain("max-width:90px");
		component.Find(".pdtabset-content").TextContent.Should().Be("One content");
		component.FindAll(".pdtabset-tab-close").Should().BeEmpty();
		component.FindAll(".pdtabset-addtab").Should().BeEmpty();
		component.Find(".pdtabset-tab-container").GetAttribute("draggable").Should().Be("false");
	}

	/// <summary>Clicking a tab selects it, shows its content and raises both selection callbacks.</summary>
	[Fact]
	public void Clicking_a_tab_selects_it_and_raises_callbacks()
	{
		var selected = new List<string>();
		var tabSelected = 0;
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.OnTabSelected, (PDTab tab) => selected.Add(tab.Title))
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two") { OnSelected = () => tabSelected++ })));

		component.FindAll("button.pdtabset-tab")[1].Click();

		selected.Should().Equal("Two");
		tabSelected.Should().Be(1);
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
		component.Find(".pdtabset-content").TextContent.Should().Be("Two content");
	}

	/// <summary>Selecting a tab with no OnTabSelected handler still selects it.</summary>
	[Fact]
	public void Selecting_without_a_handler_still_selects()
	{
		var component = Render<PDTabSet>(parameters => parameters.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));

		component.FindAll("button.pdtabset-tab")[1].Click();

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
	}

	/// <summary>SelectTabById selects a tab by its id and reports whether it was found.</summary>
	[Fact]
	public async Task SelectTabById_selects_known_ids_only()
	{
		var id = Guid.NewGuid();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two") { Id = id })));

		var found = await component.InvokeAsync(() => component.Instance.SelectTabById(id));
		found.Should().BeTrue();
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");

		(await component.InvokeAsync(() => component.Instance.SelectTabById(id))).Should().BeTrue();
		(await component.InvokeAsync(() => component.Instance.SelectTabById(Guid.NewGuid()))).Should().BeFalse();
	}

	/// <summary>A tab given no id is assigned a new one.</summary>
	[Fact]
	public void A_tab_without_an_id_is_given_one()
	{
		var component = Render<PDTabSet>(parameters => parameters.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		component.FindComponent<PDTab>().Instance.Id.Should().NotBe(Guid.Empty);
	}

	private static List<string> Titles(IRenderedComponent<PDTabSet> component)
		=> [.. component.FindAll(".pdtabset-tab-title").Select(t => t.TextContent)];

	private static RenderFragment Tabs(params Spec[] specs) => builder =>
	{
		foreach (var spec in specs)
		{
			builder.OpenComponent<PDTab>(0);
			builder.SetKey(spec.Title);
			builder.AddComponentParameter(1, nameof(PDTab.Title), spec.Title);
			builder.AddComponentParameter(2, nameof(PDTab.ChildContent), (RenderFragment)(b => b.AddContent(0, $"{spec.Title} content")));
			AddOptional(builder, spec);
			builder.CloseComponent();
		}
	};

	private static void AddOptional(RenderTreeBuilder builder, Spec spec)
	{
		if (spec.Icon is not null)
		{
			builder.AddComponentParameter(3, nameof(PDTab.IconCssClass), spec.Icon);
		}

		if (spec.Css is not null)
		{
			builder.AddComponentParameter(4, nameof(PDTab.CssClass), spec.Css);
		}

		builder.AddComponentParameter(5, nameof(PDTab.IsClosingEnabled), spec.Closing);
		builder.AddComponentParameter(6, nameof(PDTab.IsRenamingEnabled), spec.Renaming);
		if (spec.Id is { } id)
		{
			builder.AddComponentParameter(7, nameof(PDTab.Id), id);
		}

		if (spec.OnSelected is { } onSelected)
		{
			builder.AddComponentParameter(8, nameof(PDTab.OnSelected), EventCallback.Factory.Create(spec, onSelected));
		}
	}

	/// <summary>Describes one tab to render.</summary>
	/// <param name="Title">The tab title, also used as its render key.</param>
	internal sealed record Spec(string Title)
	{
		public string? Icon { get; init; }

		public string? Css { get; init; }

		public bool? Closing { get; init; }

		public bool? Renaming { get; init; }

		public Guid? Id { get; init; }

		public Action? OnSelected { get; init; }
	}
}
