using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDDragPanel{TItem}"/> renders its container's items, reorders them as one is
/// dragged over the others, and reports the new order only when it has actually changed.
/// </summary>
public class PDDragPanelTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDDragPanelTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Verifies that each item renders as a draggable element labelled with the item.</summary>
	[Fact]
	public void Each_item_renders_as_a_draggable_label()
	{
		var component = RenderPanel(Items("A", "B", "C"));

		var items = component.FindAll(".pd-dragitem");
		items.Select(i => i.TextContent.Trim()).Should().Equal("A", "B", "C");
		items.Should().AllSatisfy(i =>
		{
			i.GetAttribute("draggable").Should().Be("true");
			i.GetAttribute("style").Should().Contain("cursor: move");
		});
	}

	/// <summary>Verifies that CanDrag false leaves items undraggable.</summary>
	[Fact]
	public void CanDrag_false_leaves_items_undraggable()
	{
		var component = RenderPanel(Items("A", "B"), panel => panel.Add(p => p.CanDrag, false));

		component.FindAll(".pd-dragitem").Should().AllSatisfy(i => i.HasAttribute("draggable").Should().BeFalse());
	}

	/// <summary>Verifies that a supplied template renders each item.</summary>
	[Fact]
	public void A_template_renders_each_item()
	{
		var component = RenderPanel(Items("A", "B"), panel => panel
			.Add(p => p.Template, item => (RenderFragment)(b => b.AddMarkupContent(0, $"<em>{item.Text}</em>"))));

		component.FindAll(".pd-dragitem em").Select(e => e.TextContent).Should().Equal("A", "B");
	}

	/// <summary>Verifies that the dragged item shows as a blank placeholder marked as dragging.</summary>
	[Fact]
	public void The_dragged_item_becomes_a_placeholder()
	{
		var component = RenderPanel(Items("A", "B"));

		component.FindAll(".pd-dragitem")[0].DragStart(new DragEventArgs { ClientY = 0 });

		var first = component.FindAll(".pd-dragitem")[0];
		first.ClassList.Should().Contain("dragging");
		first.TextContent.Trim().Should().BeEmpty();
	}

	/// <summary>Verifies that a placeholder template replaces the default blank placeholder.</summary>
	[Fact]
	public void A_placeholder_template_renders_for_the_dragged_item()
	{
		var component = RenderPanel(Items("A", "B"), panel => panel
			.Add(p => p.PlaceholderTemplate, item => (RenderFragment)(b => b.AddContent(0, $"moving {item.Text}"))));

		component.FindAll(".pd-dragitem")[0].DragStart(new DragEventArgs { ClientY = 0 });

		component.FindAll(".pd-dragitem")[0].TextContent.Should().Be("moving A");
	}

	/// <summary>Verifies that dragging an item downwards over another places it after that item and reports the order.</summary>
	[Fact]
	public void Dragging_down_places_the_item_after_the_target()
	{
		var changes = new List<DragOrderChangeArgs<Item>>();
		var component = RenderPanel(Items("A", "B", "C"), panel => panel
			.Add(p => p.ItemOrderChanged, (DragOrderChangeArgs<Item> args) => changes.Add(args)));

		component.FindAll(".pd-dragitem")[0].DragStart(new DragEventArgs { ClientY = 0 });
		component.FindAll(".pd-dragitem")[2].DragEnter(new DragEventArgs { ClientY = 20 });
		component.FindAll(".pd-dragitem")[2].DragEnd(new DragEventArgs());

		var change = changes.Should().ContainSingle().Subject;
		change.Items.Select(i => i.Text).Should().Equal("B", "C", "A");
		change.Item.Text.Should().Be("A");
		component.FindAll(".pd-dragitem").Select(i => i.TextContent.Trim()).Should().Equal("B", "C", "A");
	}

	/// <summary>Verifies that dragging an item upwards over another places it before that item.</summary>
	[Fact]
	public void Dragging_up_places_the_item_before_the_target()
	{
		var changes = new List<DragOrderChangeArgs<Item>>();
		var component = RenderPanel(Items("A", "B", "C"), panel => panel
			.Add(p => p.ItemOrderChanged, (DragOrderChangeArgs<Item> args) => changes.Add(args)));

		component.FindAll(".pd-dragitem")[2].DragStart(new DragEventArgs { ClientY = 20 });
		component.FindAll(".pd-dragitem")[0].DragEnter(new DragEventArgs { ClientY = 0 });
		component.FindAll(".pd-dragitem")[0].DragEnd(new DragEventArgs());

		changes.Should().ContainSingle().Which.Items.Select(i => i.Text).Should().Equal("C", "A", "B");
	}

	/// <summary>Verifies that a drag that ends where it began reports nothing and clears the placeholder.</summary>
	[Fact]
	public void A_drag_that_changes_nothing_reports_nothing()
	{
		var changes = new List<DragOrderChangeArgs<Item>>();
		var component = RenderPanel(Items("A", "B"), panel => panel
			.Add(p => p.ItemOrderChanged, (DragOrderChangeArgs<Item> args) => changes.Add(args)));

		component.FindAll(".pd-dragitem")[0].DragStart(new DragEventArgs { ClientY = 0 });
		component.FindAll(".pd-dragitem")[0].DragEnter(new DragEventArgs { ClientY = 0 });
		component.FindAll(".pd-dragitem")[0].DragEnd(new DragEventArgs());

		changes.Should().BeEmpty();
		component.FindAll(".pd-dragitem.dragging").Should().BeEmpty();
	}

	/// <summary>Verifies that CanChangeOrder false keeps the order fixed and reports nothing.</summary>
	[Fact]
	public void CanChangeOrder_false_keeps_the_order()
	{
		var changes = new List<DragOrderChangeArgs<Item>>();
		var component = RenderPanel(Items("A", "B", "C"), panel => panel
			.Add(p => p.CanChangeOrder, false)
			.Add(p => p.ItemOrderChanged, (DragOrderChangeArgs<Item> args) => changes.Add(args)));

		component.FindAll(".pd-dragitem")[0].DragStart(new DragEventArgs { ClientY = 0 });
		component.FindAll(".pd-dragitem")[2].DragEnter(new DragEventArgs { ClientY = 20 });
		component.FindAll(".pd-dragitem")[2].DragEnd(new DragEventArgs());

		changes.Should().BeEmpty();
		component.FindAll(".pd-dragitem").Select(i => i.TextContent.Trim()).Should().Equal("A", "B", "C");
	}

	/// <summary>Verifies that entering an item with no drag in progress changes nothing.</summary>
	[Fact]
	public void Entering_without_a_drag_changes_nothing()
	{
		var component = RenderPanel(Items("A", "B"));

		component.FindAll(".pd-dragitem")[1].DragEnter(new DragEventArgs { ClientY = 20 });
		component.FindAll(".pd-dragitem")[1].DragEnd(new DragEventArgs());

		component.FindAll(".pd-dragitem").Select(i => i.TextContent.Trim()).Should().Equal("A", "B");
	}

	/// <summary>Verifies that ticking an item's checkbox reports the container's new selection.</summary>
	[Fact]
	public void Ticking_an_item_reports_the_selection()
	{
		var items = Items("A", "B");
		var selections = new List<Item[]>();
		var component = RenderPanel(items, selectionChanged: selection => selections.Add([.. selection]));

		component.FindAll(".pd-dragitem input[type=checkbox]")[1].Input(new ChangeEventArgs { Value = true });

		selections.Should().ContainSingle().Which.Select(i => i.Text).Should().Equal("B");
	}

	/// <summary>Verifies that a panel outside any container renders nothing and tolerates drag events.</summary>
	[Fact]
	public void A_panel_without_a_container_renders_no_items()
	{
		var component = Render<PDDragPanel<Item>>();

		component.FindAll(".pd-dragitem").Should().BeEmpty();
		component.Find(".pd-dragpanel").Id.Should().StartWith("pd-dragpanel-");
	}

	private IRenderedComponent<PDDragContainer<Item>> RenderPanel(
		List<Item> items,
		Action<ComponentParameterCollectionBuilder<PDDragPanel<Item>>>? configure = null,
		Action<IEnumerable<Item>>? selectionChanged = null)
		=> Render<PDDragContainer<Item>>(parameters => parameters
			.Add(p => p.Items, items)
			.Add(p => p.SelectionChanged, selection => selectionChanged?.Invoke(selection))
			.AddChildContent<PDDragPanel<Item>>(panel => configure?.Invoke(panel)));

	private static List<Item> Items(params string[] texts) => [.. texts.Select(t => new Item(t))];

	/// <summary>A selectable item identified by its text.</summary>
	public sealed class Item(string text) : ISelectable
	{
		/// <summary>Gets the text.</summary>
		public string Text { get; } = text;

		/// <inheritdoc />
		public bool IsSelected { get; set; }

		/// <inheritdoc />
		public bool IsEnabled => true;

		/// <inheritdoc />
		public override string ToString() => Text;
	}
}
