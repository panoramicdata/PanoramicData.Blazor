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

	/// <summary>Setting SelectedTabId selects that tab without reporting a change back to the host.</summary>
	[Fact]
	public void SelectedTabId_parameter_selects_a_tab_without_raising_SelectedTabIdChanged()
	{
		var id = Guid.NewGuid();
		var changes = new List<Guid?>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabIdChanged, (Guid? changed) => changes.Add(changed))
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two") { Id = id }, new Spec("Three"))));
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("One");

		component.Render(parameters => parameters.Add(p => p.SelectedTabId, id));

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
		component.Find(".pdtabset-content").TextContent.Should().Be("Two content");
		changes.Should().BeEmpty();
	}

	/// <summary>A SelectedTabId present on the first render selects that tab.</summary>
	[Fact]
	public void SelectedTabId_on_first_render_selects_that_tab()
	{
		var id = Guid.NewGuid();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabId, id)
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two") { Id = id })));

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
		component.Find(".pdtabset-content").TextContent.Should().Be("Two content");
	}

	/// <summary>A requested tab that is added later is selected when it appears.</summary>
	[Fact]
	public void SelectedTabId_selects_a_tab_added_later()
	{
		var id = Guid.NewGuid();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabId, id)
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("One");

		component.Render(parameters => parameters
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"), new Spec("Late") { Id = id })));

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Late");
		component.Find(".pdtabset-content").TextContent.Should().Be("Late content");
	}

	/// <summary>A new SelectedTabId and the tab it names arriving in the same render selects that tab.</summary>
	[Fact]
	public void SelectedTabId_and_its_tab_arriving_together_selects_the_tab()
	{
		var id = Guid.NewGuid();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));

		component.Render(parameters => parameters
			.Add(p => p.SelectedTabId, id)
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"), new Spec("Late") { Id = id })));

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Late");
	}

	/// <summary>Clicking a tab raises SelectedTabIdChanged with its id, alongside OnTabSelected.</summary>
	[Fact]
	public void Clicking_a_tab_raises_SelectedTabIdChanged_with_its_id()
	{
		var id = Guid.NewGuid();
		var changes = new List<Guid?>();
		var selected = new List<string>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabIdChanged, (Guid? changed) => changes.Add(changed))
			.Add(p => p.OnTabSelected, (PDTab tab) => selected.Add(tab.Title))
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two") { Id = id })));

		component.FindAll("button.pdtabset-tab")[1].Click();

		changes.Should().Equal(id);
		selected.Should().Equal("Two");
	}

	/// <summary>A clicked tab whose id is Guid.Empty reports null.</summary>
	[Fact]
	public void Clicking_a_tab_with_an_empty_id_raises_SelectedTabIdChanged_with_null()
	{
		var changes = new List<Guid?>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabIdChanged, (Guid? changed) => changes.Add(changed))
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));
		SetTabId(component.FindComponents<PDTab>()[1].Instance, Guid.Empty);

		component.FindAll("button.pdtabset-tab")[1].Click();

		changes.Should().ContainSingle().Which.Should().BeNull();
	}

	/// <summary>Binding SelectedTabId two ways follows a click and a later change from the host, without looping.</summary>
	[Fact]
	public void Two_way_binding_follows_clicks_and_host_changes()
	{
		var one = Guid.NewGuid();
		var two = Guid.NewGuid();
		Guid? hostValue = one;
		var changes = new List<Guid?>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabId, hostValue)
			.Add(p => p.SelectedTabIdChanged, (Guid? changed) => changes.Add(changed))
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Id = one }, new Spec("Two") { Id = two })));

		component.FindAll("button.pdtabset-tab")[1].Click();
		hostValue = changes[^1];
		component.Render(parameters => parameters.Add(p => p.SelectedTabId, hostValue));

		changes.Should().Equal(two);
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");

		hostValue = one;
		component.Render(parameters => parameters.Add(p => p.SelectedTabId, hostValue));

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("One");
		changes.Should().Equal(two);
	}

	/// <summary>Re-rendering with an unchanged SelectedTabId does not undo a tab the user has since picked.</summary>
	[Fact]
	public void An_unchanged_SelectedTabId_does_not_undo_a_user_selection()
	{
		var one = Guid.NewGuid();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabId, one)
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Id = one }, new Spec("Two"))));
		component.FindAll("button.pdtabset-tab")[1].Click();

		component.Render(parameters => parameters.Add(p => p.SelectedTabId, one));

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
	}

	/// <summary>An unknown SelectedTabId leaves the current selection unchanged and does not throw.</summary>
	[Fact]
	public void An_unknown_SelectedTabId_leaves_the_selection_unchanged()
	{
		var id = Guid.NewGuid();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two") { Id = id })));
		component.FindAll("button.pdtabset-tab")[1].Click();

		var act = () => component.Render(parameters => parameters.Add(p => p.SelectedTabId, Guid.NewGuid()));

		act.Should().NotThrow();
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
	}

	/// <summary>A pending request that never found its tab does not override a tab the user picks afterwards.</summary>
	[Fact]
	public void A_pending_request_does_not_override_a_later_user_selection()
	{
		var id = Guid.NewGuid();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabId, id)
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));
		component.FindAll("button.pdtabset-tab")[1].Click();

		component.Render(parameters => parameters
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"), new Spec("Late") { Id = id })));

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
	}

	/// <summary>Closing the selected tab raises SelectedTabIdChanged with the tab it falls back to, then null when none is left.</summary>
	[Fact]
	public void Closing_the_selected_tab_raises_SelectedTabIdChanged_with_the_fallback_tab()
	{
		var one = Guid.NewGuid();
		var two = Guid.NewGuid();
		var changes = new List<Guid?>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.SelectedTabIdChanged, (Guid? changed) => changes.Add(changed))
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Id = one }, new Spec("Two") { Id = two })));
		component.FindAll("button.pdtabset-tab")[1].Click();
		changes.Should().Equal(two);

		component.FindAll(".pdtabset-tab-close")[1].Click();

		changes.Should().Equal(two, one);
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("One");
	}

	/// <summary>Closing a tab that is not selected does not raise SelectedTabIdChanged.</summary>
	[Fact]
	public void Closing_an_unselected_tab_does_not_raise_SelectedTabIdChanged()
	{
		var changes = new List<Guid?>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.SelectedTabIdChanged, (Guid? changed) => changes.Add(changed))
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));

		component.FindAll(".pdtabset-tab-close")[1].Click();

		changes.Should().BeEmpty();
	}

	/// <summary>Closing the only tab raises SelectedTabIdChanged with null.</summary>
	[Fact]
	public void Closing_the_last_tab_raises_SelectedTabIdChanged_with_null()
	{
		var changes = new List<Guid?>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.SelectedTabIdChanged, (Guid? changed) => changes.Add(changed))
			.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		component.Find(".pdtabset-tab-close").Click();

		changes.Should().ContainSingle().Which.Should().BeNull();
	}

	/// <summary>The public SelectTabById selects a tab, reports whether it was found and raises SelectedTabIdChanged.</summary>
	[Fact]
	public async Task Public_SelectTabById_selects_and_raises_SelectedTabIdChanged()
	{
		var id = Guid.NewGuid();
		var changes = new List<Guid?>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.SelectedTabIdChanged, (Guid? changed) => changes.Add(changed))
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two") { Id = id })));

		(await component.InvokeAsync(() => component.Instance.SelectTabById(id))).Should().BeTrue();
		(await component.InvokeAsync(() => component.Instance.SelectTabById(Guid.NewGuid()))).Should().BeFalse();

		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
		changes.Should().Equal(id);
	}

	/// <summary>A tab given no id is assigned a new one.</summary>
	[Fact]
	public void A_tab_without_an_id_is_given_one()
	{
		var component = Render<PDTabSet>(parameters => parameters.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		component.FindComponent<PDTab>().Instance.Id.Should().NotBe(Guid.Empty);
	}

	private static void SetTabId(PDTab tab, Guid id)
		=> typeof(PDTab).GetProperty(nameof(PDTab.Id))!.SetValue(tab, id);

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
