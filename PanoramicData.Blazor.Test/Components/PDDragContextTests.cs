using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDDragContext"/> renders its content and cascades itself to descendants so that
/// a drag source and a drop target can share the payload being dragged.
/// </summary>
public class PDDragContextTests : BunitContext
{
	/// <summary>
	/// Verifies that the child content is rendered and receives the context as a cascading value.
	/// </summary>
	[Fact]
	public void Cascades_itself_to_child_content()
	{
		var component = Render<PDDragContext>(parameters => parameters
			.AddChildContent<ContextProbe>());

		component.Find(".probe").TextContent.Should().Be("has-context");
		component.FindComponent<ContextProbe>().Instance.Context.Should().BeSameAs(component.Instance);
	}

	/// <summary>
	/// Verifies that a payload set by one descendant is visible to another through the shared context.
	/// </summary>
	[Fact]
	public void A_payload_set_through_one_descendant_is_seen_by_another()
	{
		var component = Render<PDDragContext>(parameters => parameters
			.AddChildContent<ContextProbe>()
			.AddChildContent<ContextProbe>());

		var probes = component.FindComponents<ContextProbe>();
		probes.Should().HaveCount(2);

		var payload = new object();
		probes[0].Instance.Context!.Payload = payload;

		probes[1].Instance.Context!.Payload.Should().BeSameAs(payload);
		component.Instance.Payload.Should().BeSameAs(payload);
	}

	/// <summary>
	/// A descendant that records the drag context cascaded to it.
	/// </summary>
	private sealed class ContextProbe : ComponentBase
	{
		/// <summary>Gets or sets the cascaded drag context.</summary>
		[CascadingParameter] public PDDragContext? Context { get; set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "span");
			builder.AddAttribute(1, "class", "probe");
			builder.AddContent(2, Context is null ? "no-context" : "has-context");
			builder.CloseElement();
		}
	}
}
