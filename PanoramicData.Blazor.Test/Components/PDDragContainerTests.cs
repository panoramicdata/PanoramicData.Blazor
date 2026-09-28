using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDDragContainer{TItem}"/> exposes itself to its children and reports the selected items.
/// </summary>
public class PDDragContainerTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDDragContainerTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that the child content is rendered inside the drag container and receives the container
	/// as a cascading value.
	/// </summary>
	[Fact]
	public void ChildContent_IsWrapped_AndReceivesTheContainer()
	{
		var component = Render<PDDragContainer<Card>>(parameters => parameters
			.Add(p => p.ChildContent, builder =>
			{
				builder.OpenComponent<ContainerProbe>(0);
				builder.CloseComponent();
			}));

		component.Find("div.drag-container span.probe").TextContent.Should().Be("has-container");
		component.FindComponent<ContainerProbe>().Instance.Container.Should().BeSameAs(component.Instance);
	}

	/// <summary>
	/// Verifies that only the items whose <see cref="ISelectable.IsSelected"/> is true are reported, in order.
	/// </summary>
	[Fact]
	public async Task OnSelectionChangedAsync_RaisesOnlyTheSelectedItems()
	{
		var items = new[] { new Card("A", true), new Card("B", false), new Card("C", true) };
		IEnumerable<Card>? reported = null;

		var component = Render<PDDragContainer<Card>>(parameters => parameters
			.Add(p => p.Items, items)
			.Add(p => p.SelectionChanged, selection => reported = selection));

		await component.InvokeAsync(() => component.Instance.OnSelectionChangedAsync());

		reported.Should().NotBeNull();
		reported!.Select(c => c.Name).Should().Equal("A", "C");
	}

	/// <summary>
	/// Verifies that an empty container reports an empty selection rather than none.
	/// </summary>
	[Fact]
	public async Task OnSelectionChangedAsync_WithNoItems_RaisesAnEmptySelection()
	{
		IEnumerable<Card>? reported = null;
		var component = Render<PDDragContainer<Card>>(parameters => parameters
			.Add(p => p.SelectionChanged, selection => reported = selection));

		await component.InvokeAsync(() => component.Instance.OnSelectionChangedAsync());

		reported.Should().NotBeNull().And.BeEmpty();
	}

	/// <summary>
	/// Verifies that the payload being dragged is held by the container for its children to read.
	/// </summary>
	[Fact]
	public void Payload_HoldsTheItemBeingDragged()
	{
		var card = new Card("A", false);
		var component = Render<PDDragContainer<Card>>();

		component.Instance.Payload.Should().BeNull();
		component.Instance.Payload = card;

		component.Instance.Payload.Should().BeSameAs(card);
	}

	/// <summary>A draggable item.</summary>
	/// <param name="Name">Display name.</param>
	/// <param name="Selected">Initial selection state.</param>
	public sealed record Card(string Name, bool Selected) : ISelectable
	{
		/// <inheritdoc />
		public bool IsSelected { get; set; } = Selected;

		/// <inheritdoc />
		public bool IsEnabled => true;
	}

	/// <summary>A child that captures the cascaded container.</summary>
	private sealed class ContainerProbe : ComponentBase
	{
		[CascadingParameter]
		public PDDragContainer<Card>? Container { get; set; }

		protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "span");
			builder.AddAttribute(1, "class", "probe");
			builder.AddContent(2, Container is null ? "no-container" : "has-container");
			builder.CloseElement();
		}
	}
}
