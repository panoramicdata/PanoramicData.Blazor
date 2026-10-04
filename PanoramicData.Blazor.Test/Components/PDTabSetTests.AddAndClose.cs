using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for adding and closing tabs in <see cref="PDTabSet"/>.
/// </summary>
public partial class PDTabSetTests
{
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
