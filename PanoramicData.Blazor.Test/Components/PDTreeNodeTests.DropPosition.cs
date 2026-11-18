using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that, with drop in between allowed, the pointer's position within a row decides where a drop lands.
/// </summary>
public partial class PDTreeNodeTests
{
	// every row is measured at top 100, height 20, with its text starting at x 30
	private const double RowTop = 100;
	private const double TextLeft = 30;

	private BunitJSModuleInterop SetUpRowBounds()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.Mode = JSRuntimeMode.Loose;
		module.Setup<double[]?>("getTreeRowBounds", _ => true).SetResult([RowTop, 20, TextLeft]);
		return module;
	}

	private IRenderedComponent<PDTree<Item>> RenderReorderable(Action<ComponentParameterCollectionBuilder<PDTree<Item>>> configure)
	{
		SetUpRowBounds();
		return RenderInContext(p =>
		{
			p.Add(x => x.AllowDrag, true).Add(x => x.AllowDropInBetween, true);
			configure(p);
		});
	}

	private static AngleSharp.Dom.IElement Header(IRenderedComponent<PDTree<Item>> tree, string key)
		=> tree.Find($"#tree-node-{Node(tree, key).Id} > .pdtreenode_header");

	private static async Task HoverAsync(IRenderedComponent<PDTree<Item>> tree, string key, double offsetY, double clientX)
	{
		await Header(tree, key).DragEnterAsync(new DragEventArgs());
		await Header(tree, key).DragOverAsync(new DragEventArgs { ClientY = RowTop + offsetY, ClientX = clientX });
	}

	private static Task HoverAsync(IRenderedComponent<PDTree<Item>> tree, string key, double offsetY)
		=> HoverAsync(tree, key, offsetY, 100);

	private static Task DropAtAsync(IRenderedComponent<PDTree<Item>> tree, string key, double offsetY, double clientX)
		=> Header(tree, key).DropAsync(new DragEventArgs { ClientY = RowTop + offsetY, ClientX = clientX });

	private static Task DropAtAsync(IRenderedComponent<PDTree<Item>> tree, string key, double offsetY)
		=> DropAtAsync(tree, key, offsetY, 100);

	/// <summary>Gets the class of each child of a node's element, in order, so the line's place can be checked.</summary>
	private static List<string> ChildClasses(IRenderedComponent<PDTree<Item>> tree, string key)
		=> [.. tree.Find($"#tree-node-{Node(tree, key).Id}").Children.Select(e => (e.ClassName ?? string.Empty).Trim())];

	/// <summary>The top quarter is before, the bottom quarter after and the middle into; without into, halves.</summary>
	[Theory]
	[InlineData(0, true, DropPosition.Before)]
	[InlineData(4.9, true, DropPosition.Before)]
	[InlineData(5, true, DropPosition.Into)]
	[InlineData(14.9, true, DropPosition.Into)]
	[InlineData(15, true, DropPosition.After)]
	[InlineData(20, true, DropPosition.After)]
	[InlineData(9.9, false, DropPosition.Before)]
	[InlineData(10, false, DropPosition.After)]
	public void TreeDropZone_ChoosesThePositionFromTheOffset(double offsetY, bool allowInto, DropPosition expected)
		=> TreeDropZone.GetPosition(offsetY, 20, allowInto).Should().Be(expected);

	/// <summary>Hovering the top or bottom quarter shows an insertion line; the middle highlights the node.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Hover_ShowsTheLineOrTheIntoHighlight(bool optimizeRendering)
	{
		var tree = RenderReorderable(p => p.Add(x => x.AllowDrop, true).Add(x => x.OptimizeRendering, optimizeRendering));

		await HoverAsync(tree, "b", 2);
		ChildClasses(tree, "b")[0].Should().Be("pdtree-drop-anchor", "a before line goes above the row");
		Content(tree, "b").ClassList.Should().NotContain("drag-over");

		await HoverAsync(tree, "b", 10);
		tree.FindAll(".pdtree-drop-line").Should().BeEmpty();
		Content(tree, "b").ClassList.Should().Contain("drag-over");

		await HoverAsync(tree, "b", 18);
		ChildClasses(tree, "b").Should().ContainInOrder("pdtreenode_header", "pdtree-drop-anchor");
		Content(tree, "b").ClassList.Should().NotContain("drag-over");
	}

	/// <summary>Without AllowDrop the row splits into before and after halves.</summary>
	[Theory]
	[InlineData(9, DropPosition.Before)]
	[InlineData(11, DropPosition.After)]
	public async Task WithoutAllowDrop_TheRowSplitsInHalves(double offsetY, DropPosition expected)
	{
		var tree = RenderReorderable(_ => { });
		await Content(tree, "a").DragStartAsync(new DragEventArgs());

		await DropAtAsync(tree, "b", offsetY);

		_drops.Should().ContainSingle().Which.Position.Should().Be(expected);
	}

	/// <summary>The drop reports its position, and Before still says the same thing.</summary>
	[Theory]
	[InlineData(2, DropPosition.Before, true)]
	[InlineData(10, DropPosition.Into, null)]
	[InlineData(18, DropPosition.After, false)]
	public async Task Drop_ReportsThePosition_AndBeforeAgrees(double offsetY, DropPosition position, bool? before)
	{
		var tree = RenderReorderable(p => p.Add(x => x.AllowDrop, true));
		await Content(tree, "a").DragStartAsync(new DragEventArgs());
		await HoverAsync(tree, "b", offsetY);

		await DropAtAsync(tree, "b", offsetY);

		var drop = _drops.Should().ContainSingle().Subject;
		drop.Target.Should().BeSameAs(Node(tree, "b"));
		drop.Position.Should().Be(position);
		drop.Before.Should().Be(before);
		drop.Payload.Should().BeSameAs(tree.Instance.DragContext!.Payload);
		tree.FindAll(".pdtree-drop-line").Should().BeEmpty("the line is cleared on drop");
		Content(tree, "b").ClassList.Should().NotContain("drag-over");
	}

	/// <summary>Leaving the row clears the line.</summary>
	[Fact]
	public async Task Leave_ClearsTheLine()
	{
		var tree = RenderReorderable(_ => { });
		await HoverAsync(tree, "b", 2);
		tree.FindAll(".pdtree-drop-line").Should().ContainSingle();

		await Header(tree, "b").DragLeaveAsync(new DragEventArgs());

		tree.FindAll(".pdtree-drop-line").Should().BeEmpty();
	}

	/// <summary>A consumer can refuse into while allowing before and after: the middle then splits in halves.</summary>
	[Fact]
	public async Task IsDropPositionValid_RefusingInto_SplitsTheMiddle()
	{
		var asked = new List<DropPosition>();
		var tree = RenderReorderable(p => p
			.Add(x => x.AllowDrop, true)
			.Add(x => x.IsDropPositionValid, (Item _, object? _, DropPosition position) =>
			{
				asked.Add(position);
				return position != DropPosition.Into;
			}));
		await Content(tree, "a").DragStartAsync(new DragEventArgs());

		await HoverAsync(tree, "b", 8);
		tree.FindAll(".pdtree-drop-line:not(.invalid)").Should().ContainSingle();
		await DropAtAsync(tree, "b", 8);

		_drops.Should().ContainSingle().Which.Position.Should().Be(DropPosition.Before);
		asked.Should().Contain([DropPosition.Into, DropPosition.Before]);
	}

	/// <summary>A refused before or after shows an invalid line and raises no drop.</summary>
	[Fact]
	public async Task IsDropPositionValid_RefusingBefore_ShowsInvalidAndDropsNothing()
	{
		var tree = RenderReorderable(p => p
			.Add(x => x.IsDropPositionValid, (Item _, object? _, DropPosition position) => position != DropPosition.Before));
		await Content(tree, "a").DragStartAsync(new DragEventArgs());

		await HoverAsync(tree, "b", 2);
		tree.Find(".pdtree-drop-line").ClassList.Should().Contain("invalid");
		await DropAtAsync(tree, "b", 2);

		_drops.Should().BeEmpty();
	}

	/// <summary>The existing IsDropValid still governs a drop into a node, and refusing it leaves before and after.</summary>
	[Fact]
	public async Task IsDropValid_RefusingInto_LeavesBeforeAndAfter()
	{
		var tree = RenderReorderable(p => p
			.Add(x => x.AllowDrop, true)
			.Add(x => x.IsDropValid, (Item _, object? _) => false));
		await Content(tree, "a").DragStartAsync(new DragEventArgs());

		await DropAtAsync(tree, "b", 12);

		_drops.Should().ContainSingle().Which.Position.Should().Be(DropPosition.After);
	}

	/// <summary>After an expanded parent means after its subtree, so the line is drawn below its children.</summary>
	[Fact]
	public async Task AfterAnExpandedParent_TheLineFollowsItsSubtree()
	{
		var tree = RenderReorderable(_ => { });
		await ExpandAsync(tree, "a");

		await HoverAsync(tree, "a", 18);

		ChildClasses(tree, "a").Should().ContainInOrder("pdtreenode_header", "pdtreenode_child_content", "pdtree-drop-anchor");
		await DropAtAsync(tree, "a", 18);
		var drop = _drops.Should().ContainSingle().Subject;
		drop.Target.Should().BeSameAs(Node(tree, "a"));
		drop.Position.Should().Be(DropPosition.After);
	}

	/// <summary>
	/// With AllowDropAfterParent, the bottom of the last child's row left of its text means after the parent.
	/// </summary>
	[Theory]
	[InlineData(true, 10, "a")]
	[InlineData(true, 50, "a1")]
	[InlineData(false, 10, "a1")]
	public async Task LastChild_PointerLeftOfItsText_MeansAfterTheParent(bool allowAfterParent, double clientX, string expectedKey)
	{
		var tree = RenderReorderable(p => p.Add(x => x.AllowDropAfterParent, allowAfterParent));
		await ExpandAsync(tree, "a");

		await HoverAsync(tree, "a1", 18, clientX);
		ChildClasses(tree, expectedKey).Should().Contain("pdtree-drop-anchor");
		await DropAtAsync(tree, "a1", 18, clientX);

		var drop = _drops.Should().ContainSingle().Subject;
		drop.Target.Should().BeSameAs(Node(tree, expectedKey));
		drop.Position.Should().Be(DropPosition.After);
	}

	/// <summary>A drop on a separator reports its position and is subject to the per-position check.</summary>
	[Fact]
	public void DropOnASeparator_ReportsThePosition_AndHonoursTheCheck()
	{
		var tree = RenderReorderable(p => p
			.Add(x => x.IsDropPositionValid, (Item _, object? _, DropPosition position) => position != DropPosition.After));

		tree.FindAll(".pdseparator.after")[^1].Drop();
		tree.FindAll(".pdseparator.before")[0].Drop();

		_drops.Should().ContainSingle().Which.Position.Should().Be(DropPosition.Before);
	}

	/// <summary>A tree without drop in between is unchanged: no rows are measured and a drop is into the node.</summary>
	[Fact]
	public async Task WithoutDropInBetween_RowsAreNotMeasured()
	{
		var module = SetUpRowBounds();
		var tree = RenderInContext(p => p.Add(x => x.AllowDrop, true).Add(x => x.AllowDrag, true));
		await Content(tree, "a").DragStartAsync(new DragEventArgs());

		await Content(tree, "b").DragEnterAsync(new DragEventArgs());
		Content(tree, "b").ClassList.Should().Contain("drag-over");
		await Content(tree, "b").DropAsync(new DragEventArgs { ClientY = RowTop + 2 });

		module.VerifyNotInvoke("getTreeRowBounds");
		tree.Find($"#tree-node-{Node(tree, "b").Id}").ClassName.Should().Be("pdtreenode");
		_drops.Should().ContainSingle().Which.Position.Should().Be(DropPosition.Into);
	}
}
