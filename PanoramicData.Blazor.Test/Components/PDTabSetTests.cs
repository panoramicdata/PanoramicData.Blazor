using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTabSet"/> selects, closes, adds, renames and reorders tabs, and raises the matching
/// callbacks.
/// </summary>
/// <remarks>Dynamic add and remove of tabs from a collection is covered by <see cref="PDTabSetDynamicTabsTests"/>.</remarks>
public class PDTabSetTests : BunitContext
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

	/// <summary>Closing a non-active tab removes it and raises OnTabClosed, leaving the active tab alone.</summary>
	[Fact]
	public async Task Closing_a_tab_removes_it_and_raises_OnTabClosed()
	{
		var closed = new List<string>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.OnTabClosed, (PDTab tab) => closed.Add(tab.Title))
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));
		var two = component.FindComponents<PDTab>()[1].Instance;

		await component.InvokeAsync(() => component.Instance.CloseTab(two));

		closed.Should().Equal("Two");
		component.FindAll(".pdtabset-tab-title").Select(t => t.TextContent).Should().Equal("One");
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("One");
	}

	/// <summary>Closing the active tab makes the first remaining tab active; closing an unknown tab does nothing.</summary>
	[Fact]
	public async Task Closing_the_active_tab_selects_the_first_remaining()
	{
		var closed = 0;
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.OnTabClosed, (PDTab _) => closed++)
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));
		var one = component.FindComponents<PDTab>()[0].Instance;

		await component.InvokeAsync(() => component.Instance.CloseTab(one));
		await component.InvokeAsync(() => component.Instance.CloseTab(one));

		closed.Should().Be(1);
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
	}

	/// <summary>Closing without an OnTabClosed handler still removes the tab.</summary>
	[Fact]
	public async Task Closing_without_a_handler_still_removes_the_tab()
	{
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));
		var two = component.FindComponents<PDTab>()[1].Instance;

		await component.InvokeAsync(() => component.Instance.CloseTab(two));

		component.FindAll(".pdtabset-tab-title").Should().ContainSingle();
	}

	/// <summary>A tab's own closing and renaming settings override the tab set's.</summary>
	[Fact]
	public void Per_tab_settings_override_the_tab_set()
	{
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Closing = false }, new Spec("Two"))));

		var buttons = component.FindAll("button.pdtabset-tab");
		buttons[0].QuerySelector(".pdtabset-tab-close").Should().BeNull();
		buttons[1].QuerySelector(".pdtabset-tab-close").Should().NotBeNull();

		var tabs = component.FindComponents<PDTab>().Select(c => c.Instance).ToList();
		component.Instance.GetTabCanBeClosed(tabs[0]).Should().BeFalse();
		component.Instance.GetTabCanBeRenamed(tabs[0]).Should().BeFalse();
	}

	/// <summary>The add button is placed at the start, the end or both, and reports which one was pressed.</summary>
	[Theory]
	[InlineData(CreateTabPosition.Start, 1, true)]
	[InlineData(CreateTabPosition.End, 1, false)]
	[InlineData(CreateTabPosition.Both, 2, true)]
	public void Add_buttons_follow_the_create_position(CreateTabPosition position, int expectedButtons, bool firstIsStart)
	{
		var added = new List<CreateTabPosition>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabAdditionEnabled, true)
			.Add(p => p.CreateTabPosition, position)
			.Add(p => p.OnTabAdded, (CreateTabPosition pos) => added.Add(pos))
			.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		var addButtons = component.FindAll(".pdtabset-addtab");
		addButtons.Should().HaveCount(expectedButtons);
		var containers = component.FindAll(".pdtabset-tab-container");
		containers[0].ClassList.Contains("pdtabset-addtab-container").Should().Be(firstIsStart);

		addButtons[0].Click();

		added.Should().Equal(firstIsStart ? CreateTabPosition.Start : CreateTabPosition.End);
	}

	/// <summary>Pressing the add button with no OnTabAdded handler does nothing visible.</summary>
	[Fact]
	public void Add_without_a_handler_changes_nothing()
	{
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabAdditionEnabled, true)
			.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		component.Find(".pdtabset-addtab").Click();

		component.FindAll(".pdtabset-tab-title").Should().ContainSingle();
	}

	/// <summary>A tab added through the start button by its host is inserted at the start of the strip.</summary>
	[Fact]
	public void A_tab_added_from_the_start_button_is_inserted_first()
	{
		var host = Render<AddingHost>(parameters => parameters.Add(p => p.Position, CreateTabPosition.Start));

		host.Find(".pdtabset-addtab").Click();

		host.WaitForAssertion(() => host.FindAll(".pdtabset-tab-title").Select(t => t.TextContent)
			.Should().Equal("New 1", "Existing"));
	}

	/// <summary>A tab added through the end button by its host is appended.</summary>
	[Fact]
	public void A_tab_added_from_the_end_button_is_appended()
	{
		var host = Render<AddingHost>(parameters => parameters.Add(p => p.Position, CreateTabPosition.End));

		host.Find(".pdtabset-addtab").Click();

		host.WaitForAssertion(() => host.FindAll(".pdtabset-tab-title").Select(t => t.TextContent)
			.Should().Equal("Existing", "New 1"));
	}

	/// <summary>Clicking the close mark of the active tab closes it and leaves the other tab active.</summary>
	[Fact]
	public void Clicking_the_close_mark_closes_the_tab()
	{
		var closed = new List<string>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.OnTabClosed, (PDTab tab) => closed.Add(tab.Title))
			.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"))));

		component.FindAll(".pdtabset-tab-close")[0].Click();

		closed.Should().Equal("One");
		Titles(component).Should().Equal("Two");
		component.Find("button.pdtabset-tab.active .pdtabset-tab-title").TextContent.Should().Be("Two");
		component.Find(".pdtabset-content").TextContent.Should().Be("Two content");
	}

	/// <summary>Double-clicking a renamable tab shows the rename box; Enter commits and raises OnTabRenamed.</summary>
	[Fact]
	public void Rename_with_enter_commits_the_new_title()
	{
		var renamed = new List<string>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabRenamingEnabled, true)
			.Add(p => p.IsTabClosingEnabled, true)
			.Add(p => p.OnTabRenamed, (PDTab tab) => renamed.Add(tab.Title))
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Icon = "fas fa-1" })));

		component.Find("button.pdtabset-tab i.pdtabset-tab-icon").Should().NotBeNull();
		component.Find("button.pdtabset-tab .pdtabset-tab-close").Should().NotBeNull();
		component.Find("button.pdtabset-tab").DoubleClick();

		var input = component.Find("input.pdtabset-tab-rename-input");
		input.GetAttribute("value").Should().Be("One");
		input.Input("Renamed");
		input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

		renamed.Should().Equal("Renamed");
		component.FindAll("input.pdtabset-tab-rename-input").Should().BeEmpty();
	}

	/// <summary>Leaving the rename box commits the new title and ends the rename.</summary>
	[Fact]
	public void Rename_on_blur_commits_the_new_title()
	{
		var renamed = new List<string>();
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabRenamingEnabled, true)
			.Add(p => p.OnTabRenamed, (PDTab tab) => renamed.Add(tab.Title))
			.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		component.Find("button.pdtabset-tab").DoubleClick();
		var input = component.Find("input.pdtabset-tab-rename-input");
		input.Input("Blurred");
		input.Blur();

		renamed.Should().Equal("Blurred");
		component.FindAll("input.pdtabset-tab-rename-input").Should().BeEmpty();
	}

	/// <summary>A commit with no OnTabRenamed handler still ends the rename.</summary>
	[Fact]
	public void Rename_without_a_handler_ends_the_rename()
	{
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.IsTabRenamingEnabled, true)
			.Add(p => p.ChildContent, Tabs(new Spec("One"))));

		component.Find("button.pdtabset-tab").DoubleClick();
		component.Find("input.pdtabset-tab-rename-input").Input("Other");
		component.Find("input.pdtabset-tab-rename-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		component.FindAll("input.pdtabset-tab-rename-input").Should().BeEmpty();
	}

	/// <summary>Escape abandons the rename, and committing an unchanged title raises nothing.</summary>
	[Fact]
	public void Rename_escape_and_unchanged_commit_raise_nothing()
	{
		var renamed = 0;
		var component = Render<PDTabSet>(parameters => parameters
			.Add(p => p.OnTabRenamed, (PDTab _) => renamed++)
			.Add(p => p.ChildContent, Tabs(new Spec("One") { Renaming = true })));

		component.Find("button.pdtabset-tab").DoubleClick();
		component.Find("input.pdtabset-tab-rename-input").Input("Abandoned");
		component.Find("input.pdtabset-tab-rename-input").KeyDown(new KeyboardEventArgs { Key = "Escape" });
		component.Find(".pdtabset-tab-title").TextContent.Should().Be("One");

		component.Find("button.pdtabset-tab").DoubleClick();
		component.Find("input.pdtabset-tab-rename-input").KeyDown(new KeyboardEventArgs { Key = "a" });
		component.FindAll("input.pdtabset-tab-rename-input").Should().ContainSingle();
		component.Find("input.pdtabset-tab-rename-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		renamed.Should().Be(0);
		component.Find(".pdtabset-tab-title").TextContent.Should().Be("One");
	}

	/// <summary>An input event with no value clears the pending title.</summary>
	[Fact]
	public void Rename_input_with_null_value_clears_the_pending_title()
	{
		var component = Render<PDTabSet>(parameters => parameters.Add(p => p.ChildContent, Tabs(new Spec("One"))));
		var tab = component.FindComponent<PDTab>().Instance;

		PDTabSet.OnRenameTabInput(tab, new ChangeEventArgs { Value = null });

		tab.TempTitle.Should().BeEmpty();
	}

	/// <summary>A tab that cannot be renamed ignores a request to start renaming.</summary>
	[Fact]
	public async Task StartRenamingTab_is_ignored_when_renaming_is_disabled()
	{
		var component = Render<PDTabSet>(parameters => parameters.Add(p => p.ChildContent, Tabs(new Spec("One"))));
		var tab = component.FindComponent<PDTab>().Instance;

		await component.InvokeAsync(() => component.Instance.StartRenamingTab(tab));

		tab.IsRenaming.Should().BeFalse();
		component.FindAll("input.pdtabset-tab-rename-input").Should().BeEmpty();
	}

	/// <summary>Dragging a tab right onto another moves it and raises OnTabsReordered with the new order.</summary>
	[Fact]
	public void Dragging_right_reorders_the_tabs()
	{
		IReadOnlyList<PDTab>? reordered = null;
		var component = RenderReorderable(tabs => reordered = tabs);
		component.Find(".pdtabset-tabs").ClassList.Should().Contain("reordering");
		component.Find(".pdtabset-tab-container").GetAttribute("draggable").Should().Be("true");

		var containers = component.FindAll(".pdtabset-tab-container");
		containers[0].DragStart();
		component.Find("button.pdtabset-tab").ClassList.Should().Contain("dragging");
		component.FindAll(".pdtabset-tab-container")[2].DragOver();
		component.FindAll(".pdtabset-tab-container")[2].ClassList.Should().Contain("drag-over");
		component.FindAll(".pdtabset-tab-container")[2].Drop();

		Titles(component).Should().Equal("Two", "One", "Three");
		reordered!.Select(t => t.Title).Should().Equal("Two", "One", "Three");
		component.FindAll(".drag-over").Should().BeEmpty();
		component.FindAll(".dragging").Should().BeEmpty();
	}

	/// <summary>Dragging a tab left onto another places it in that tab's slot.</summary>
	[Fact]
	public void Dragging_left_reorders_the_tabs()
	{
		var component = RenderReorderable(null);

		component.FindAll(".pdtabset-tab-container")[2].DragStart();
		component.FindAll(".pdtabset-tab-container")[0].Drop();

		Titles(component).Should().Equal("Three", "One", "Two");
	}

	/// <summary>Dropping a tab on itself, dragging over itself, or ending a drag changes nothing.</summary>
	[Fact]
	public void Dropping_on_itself_or_ending_the_drag_changes_nothing()
	{
		var raised = 0;
		var component = RenderReorderable(_ => raised++);

		component.FindAll(".pdtabset-tab-container")[1].DragOver();
		component.FindAll(".drag-over").Should().BeEmpty();

		component.FindAll(".pdtabset-tab-container")[1].DragStart();
		component.FindAll(".pdtabset-tab-container")[1].DragOver();
		component.FindAll(".drag-over").Should().BeEmpty();
		component.FindAll(".pdtabset-tab-container")[0].DragOver();
		component.FindAll(".pdtabset-tab-container")[0].DragOver();
		component.FindAll(".pdtabset-tab-container")[1].Drop();

		component.FindAll(".pdtabset-tab-container")[0].DragStart();
		component.FindAll(".pdtabset-tab-container")[0].DragEnd();
		component.FindAll(".dragging").Should().BeEmpty();

		Titles(component).Should().Equal("One", "Two", "Three");
		raised.Should().Be(0);
	}

	/// <summary>A reorder with no OnTabsReordered handler still reorders.</summary>
	[Fact]
	public void Reorder_without_a_handler_still_reorders()
	{
		var component = RenderReorderable(null);

		component.FindAll(".pdtabset-tab-container")[1].DragStart();
		component.FindAll(".pdtabset-tab-container")[0].Drop();

		Titles(component).Should().Equal("Two", "One", "Three");
	}

	private IRenderedComponent<PDTabSet> RenderReorderable(Action<IReadOnlyList<PDTab>>? onReordered)
		=> Render<PDTabSet>(parameters =>
		{
			parameters
				.Add(p => p.IsTabReorderingEnabled, true)
				.Add(p => p.ChildContent, Tabs(new Spec("One"), new Spec("Two"), new Spec("Three")));
			if (onReordered is not null)
			{
				parameters.Add(p => p.OnTabsReordered, onReordered);
			}
		});

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

	/// <summary>A host that owns the tab titles and adds one each time the tab set's add button is pressed.</summary>
	private sealed class AddingHost : ComponentBase
	{
		private readonly List<string> _titles = ["Existing"];

		[Parameter]
		public CreateTabPosition Position { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<PDTabSet>(0);
			builder.AddComponentParameter(1, nameof(PDTabSet.IsTabAdditionEnabled), true);
			builder.AddComponentParameter(2, nameof(PDTabSet.CreateTabPosition), Position);
			builder.AddComponentParameter(3, nameof(PDTabSet.OnTabAdded), EventCallback.Factory.Create<CreateTabPosition>(this, OnAdded));
			builder.AddComponentParameter(4, nameof(PDTabSet.ChildContent), Tabs([.. _titles.Select(t => new Spec(t))]));
			builder.CloseComponent();
		}

		// Always appended here, so any placement at the start is the tab set's own doing.
		private void OnAdded()
			=> _titles.Add($"New {_titles.Count}");
	}
}
