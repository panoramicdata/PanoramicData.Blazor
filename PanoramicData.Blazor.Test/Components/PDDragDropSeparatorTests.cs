using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDDragDropSeparator"/> renders its position and size, highlights while an item is
/// dragged over it, and reports drops with the payload of the enclosing <see cref="PDDragContext"/>.
/// </summary>
public class PDDragDropSeparatorTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDDragDropSeparatorTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>By default the separator is three pixels high, sits after its item and is not highlighted.</summary>
	[Fact]
	public void Defaults_RenderAfterSeparatorThreePixelsHigh()
	{
		var cut = Render<PDDragDropSeparator>();

		var div = cut.Find("div.pdseparator");
		div.GetAttribute("style").Should().Be("height: 3px");
		div.ClassList.Should().Contain("after").And.NotContain("before").And.NotContain("dragover");
	}

	/// <summary>Height, position and an extra CSS class are all reflected in the markup.</summary>
	[Fact]
	public void Parameters_AreReflectedInMarkup()
	{
		var cut = Render<PDDragDropSeparator>(p => p
			.Add(x => x.Height, 7)
			.Add(x => x.Before, true)
			.Add(x => x.CssClass, "extra"));

		var div = cut.Find("div.pdseparator");
		div.GetAttribute("style").Should().Be("height: 7px");
		div.ClassList.Should().Contain("before").And.Contain("extra").And.NotContain("after");
	}

	/// <summary>Dragging over the separator highlights it, and dragging away removes the highlight.</summary>
	[Fact]
	public void DragOverThenLeave_TogglesHighlight()
	{
		var cut = Render<PDDragDropSeparator>();

		cut.Find("div").DragOver();
		cut.Instance.DragOver.Should().BeTrue();
		cut.Find("div").ClassList.Should().Contain("dragover");

		cut.Find("div").DragLeave();
		cut.Instance.DragOver.Should().BeFalse();
		cut.Find("div").ClassList.Should().NotContain("dragover");
	}

	/// <summary>A drop clears the highlight and raises <see cref="PDDragDropSeparator.Drop"/> with the drag payload, Ctrl state and position.</summary>
	[Fact]
	public void Drop_InsideDragContext_RaisesDropWithPayload()
	{
		DropEventArgs? received = null;
		var context = Render<PDDragContext>(p => p.AddChildContent<PDDragDropSeparator>(s => s
			.Add(x => x.Before, false)
			.Add(x => x.Drop, args => received = args)));
		context.Instance.Payload = "payload";
		var separator = context.FindComponent<PDDragDropSeparator>();
		separator.Find("div").DragOver();

		separator.Find("div").Drop(new DragEventArgs { CtrlKey = true });

		received.Should().NotBeNull();
		received!.Payload.Should().Be("payload");
		received.Ctrl.Should().BeTrue();
		received.Before.Should().BeFalse();
		received.Target.Should().BeSameAs(separator.Instance);
		separator.Instance.DragOver.Should().BeFalse();
	}

	/// <summary>A drop outside any drag context raises the event with no payload and the null position left unchanged.</summary>
	[Fact]
	public void Drop_WithoutDragContext_RaisesDropWithNullPayload()
	{
		DropEventArgs? received = null;
		var cut = Render<PDDragDropSeparator>(p => p.Add(x => x.Drop, args => received = args));

		cut.Find("div").Drop(new DragEventArgs());

		received.Should().NotBeNull();
		received!.Payload.Should().BeNull();
		received.Ctrl.Should().BeFalse();
		received.Before.Should().BeNull();
	}
}
