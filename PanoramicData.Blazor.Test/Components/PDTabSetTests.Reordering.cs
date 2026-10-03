using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Drag-and-drop tab reordering tests for <see cref="PDTabSet"/>.
/// </summary>
public partial class PDTabSetTests
{
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
}
