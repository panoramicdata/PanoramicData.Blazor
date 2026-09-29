using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDTreeNode{TItem}"/> renders a node's expander, content and children, and handles
/// dragging and dropping within a <see cref="PDDragContext"/>.
/// </summary>
public class PDTreeNodeTests : BunitContext
{
	private readonly List<DropEventArgs> _drops = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDTreeNodeTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDTree<Item>> RenderInContext(Action<ComponentParameterCollectionBuilder<PDTree<Item>>>? configure = null)
	{
		var context = Render<PDDragContext>(parameters => parameters.AddChildContent<PDTree<Item>>(tree =>
		{
			tree
				.Add(p => p.DataProvider, new ListDataProvider())
				.Add(p => p.KeyField, item => item.Id)
				.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty)
				.Add(p => p.TextField, item => item.Name)
				.Add(p => p.AllowSelection, true)
				.Add(p => p.Drop, (DropEventArgs e) => _drops.Add(e));
			configure?.Invoke(tree);
		}));
		var component = context.FindComponent<PDTree<Item>>();
		component.WaitForAssertion(() => component.FindAll(".pdtreenode").Should().NotBeEmpty());
		return component;
	}

	private static TreeNode<Item> Node(IRenderedComponent<PDTree<Item>> tree, string key)
		=> tree.Instance.RootNode.Find(key)!;

	private static AngleSharp.Dom.IElement Content(IRenderedComponent<PDTree<Item>> tree, string key)
		=> tree.Find($"#pdtnc-{Node(tree, key).Id}");

	private static async Task ExpandAsync(IRenderedComponent<PDTree<Item>> tree, string key)
	{
		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(Node(tree, key)));
		tree.Render();
	}

	/// <summary>A branch shows a clickable expander that toggles its children; a leaf shows a hidden one.</summary>
	[Fact]
	public void Expander_TogglesChildren_AndIsHiddenForLeaves()
	{
		var tree = RenderInContext();
		var expander = tree.Find($"#tree-node-{Node(tree, "a").Id} > .pdtreenode_header > i");
		expander.ClassList.Should().Contain(["fa-plus-square", "pd-pointer"]);
		tree.Markup.Should().NotContain("Alpha one");

		expander.Click();

		tree.Find($"#tree-node-{Node(tree, "a").Id} > .pdtreenode_header > i").ClassList.Should().Contain("fa-minus-square");
		tree.Markup.Should().Contain("Alpha one");
		tree.Find($"#tree-node-{Node(tree, "b").Id} > .pdtreenode_header > i").ClassList.Should().Contain("fa-hidden");
	}

	/// <summary>ShowLines marks a branch's child area; an icon class adds an icon before the text.</summary>
	[Fact]
	public void ShowLinesAndIcons_AreRendered()
	{
		var tree = RenderInContext(p => p
			.Add(x => x.ShowLines, true)
			.Add(x => x.IconCssClass, (Item _, int _) => "fas fa-folder"));

		tree.FindAll(".pdtreenode_child_content").Should().AllSatisfy(e => e.ClassList.Should().Contain("pdtree_lines"));
		Content(tree, "a").QuerySelector("i.fa-folder").Should().NotBeNull();
	}

	/// <summary>With dragging allowed the content is draggable and a drag image is registered with JS.</summary>
	[Fact]
	public void AllowDrag_MakesContentDraggable_AndRegistersADragImage()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.Mode = JSRuntimeMode.Loose;

		var tree = RenderInContext(p => p.Add(x => x.AllowDrag, true));

		Content(tree, "a").GetAttribute("draggable").Should().Be("true");
		tree.WaitForAssertion(() => module.Invocations["initDragImage"].Select(i => i.Arguments[0])
			.Should().Contain($"pdtnc-{Node(tree, "a").Id}"));
	}

	/// <summary>Without dragging the content is not draggable.</summary>
	[Fact]
	public void NoDrag_ContentIsNotDraggable()
	{
		var tree = RenderInContext();

		Content(tree, "a").HasAttribute("draggable").Should().BeFalse();
	}

	/// <summary>Starting an edit selects the edit box text through JS, and the content stops being draggable.</summary>
	[Fact]
	public async Task BeginEdit_SelectsTheEditText()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.Mode = JSRuntimeMode.Loose;
		var tree = RenderInContext(p => p.Add(x => x.AllowEdit, true).Add(x => x.AllowDrag, true));
		var node = Node(tree, "b");
		await tree.InvokeAsync(() => tree.Instance.SelectNode(node, false));

		await tree.InvokeAsync(tree.Instance.BeginEdit);

		tree.WaitForAssertion(() => module.VerifyInvoke("selectText").Arguments.Should().Equal($"PDTNE{node.Id}", 0, "Bravo".Length));
		Content(tree, "b").HasAttribute("draggable").Should().BeFalse();
	}

	/// <summary>Dragging a node puts its item in the drag context as the payload.</summary>
	[Fact]
	public void DragStart_SetsThePayload()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDrag, true));

		Content(tree, "b").DragStart();

		var payload = tree.Instance.DragContext!.Payload.Should().BeOfType<List<Item>>().Subject;
		payload.Should().ContainSingle().Which.Id.Should().Be("b");
	}

	/// <summary>Dragging over a drop target highlights it, and leaving removes the highlight.</summary>
	[Fact]
	public void DragEnterAndLeave_HighlightTheTarget()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDrop, true));

		Content(tree, "a").DragEnter();
		Content(tree, "a").ClassList.Should().Contain("drag-over");

		Content(tree, "a").DragLeave();
		Content(tree, "a").ClassList.Should().NotContain("drag-over");

		Content(tree, "a").DragLeave();
		Content(tree, "a").ClassList.Should().NotContain("drag-over", "the count never goes below zero");
	}

	/// <summary>A target the validity check rejects is highlighted as invalid.</summary>
	[Fact]
	public void DragEnter_OnAnInvalidTarget_HighlightsItAsInvalid()
	{
		var tree = RenderInContext(p => p
			.Add(x => x.AllowDrop, true)
			.Add(x => x.IsDropValid, (Item target, object? _) => target.Id != "a"));

		Content(tree, "a").DragEnter();
		Content(tree, "b").DragEnter();

		Content(tree, "a").ClassList.Should().Contain("drag-over-invalid");
		Content(tree, "b").ClassList.Should().Contain("drag-over");
	}

	/// <summary>Without AllowDrop, dragging over a node does not highlight it.</summary>
	[Fact]
	public void DragEnter_WithoutAllowDrop_DoesNothing()
	{
		var tree = RenderInContext();

		Content(tree, "a").DragEnter();

		Content(tree, "a").ClassList.Should().NotContain(["drag-over", "drag-over-invalid"]);
	}

	/// <summary>Dropping onto a valid node raises Drop with the node, the payload and the ctrl key.</summary>
	[Fact]
	public void Drop_OnAValidNode_RaisesDrop()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDrop, true).Add(x => x.AllowDrag, true));
		Content(tree, "b").DragStart();

		Content(tree, "a").DragEnter();
		Content(tree, "a").Drop(new DragEventArgs { CtrlKey = true });

		var drop = _drops.Should().ContainSingle().Subject;
		drop.Target.Should().BeSameAs(Node(tree, "a"));
		drop.Payload.Should().BeSameAs(tree.Instance.DragContext!.Payload);
		drop.Ctrl.Should().BeTrue();
		drop.Before.Should().BeNull();
		Content(tree, "a").ClassList.Should().NotContain("drag-over");
	}

	/// <summary>Dropping onto a node the validity check rejects raises nothing.</summary>
	[Fact]
	public void Drop_OnAnInvalidNode_RaisesNothing()
	{
		var tree = RenderInContext(p => p
			.Add(x => x.AllowDrop, true)
			.Add(x => x.IsDropValid, (Item _, object? _) => false));

		Content(tree, "a").Drop();

		_drops.Should().BeEmpty();
	}

	/// <summary>A drop onto a nested node is passed up through its ancestors to the tree.</summary>
	[Fact]
	public async Task Drop_OnANestedNode_ReachesTheTree()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDrop, true));
		await ExpandAsync(tree, "a");
		await ExpandAsync(tree, "a1");

		Content(tree, "a1x").Drop();

		_drops.Should().ContainSingle().Which.Target.Should().BeSameAs(Node(tree, "a1x"));
	}

	/// <summary>A drop onto a nested node with a node template is also passed up to the tree.</summary>
	[Fact]
	public async Task Drop_OnANestedTemplatedNode_ReachesTheTree()
	{
		var tree = RenderInContext(p => p
			.Add(x => x.AllowDrop, true)
			.Add(x => x.NodeTemplate, node => b => b.AddContent(0, $"[{node.Key}]")));
		await ExpandAsync(tree, "a");

		tree.Markup.Should().Contain("[a1]");
		Content(tree, "a1").Drop();

		_drops.Should().ContainSingle().Which.Target.Should().BeSameAs(Node(tree, "a1"));
	}

	/// <summary>With drop in between, separators appear before each node and after the last sibling.</summary>
	[Fact]
	public void DropInBetween_RendersSeparators()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDropInBetween, true));

		tree.FindAll(".pdseparator.before").Should().HaveCount(2);
		tree.FindAll(".pdseparator.after").Should().ContainSingle("only the last top-level node has an after separator");
	}

	/// <summary>A drop on a separator raises Drop for that node with the before or after position.</summary>
	[Theory]
	[InlineData(".pdseparator.before", true, "a")]
	[InlineData(".pdseparator.after", false, "b")]
	public void DropOnASeparator_RaisesDropWithThePosition(string selector, bool before, string expectedKey)
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDropInBetween, true));

		tree.FindAll(selector)[before ? 0 : ^1].Drop();

		var drop = _drops.Should().ContainSingle().Subject;
		drop.Before.Should().Be(before);
		drop.Target.Should().BeSameAs(Node(tree, expectedKey));
	}

	/// <summary>An item in the tree under test.</summary>
	/// <param name="Id">Key.</param>
	/// <param name="ParentId">Parent key, or null at the top level.</param>
	/// <param name="Name">Display text.</param>
	public sealed record Item(string Id, string? ParentId, string Name);

	/// <summary>Returns a fixed set of items.</summary>
	private sealed class ListDataProvider : DataProviderBase<Item>
	{
		private readonly List<Item> _items =
		[
			new("a", null, "Alpha"),
			new("a1", "a", "Alpha one"),
			new("a1x", "a1", "Alpha one x"),
			new("b", null, "Bravo")
		];

		public override Task<DataResponse<Item>> GetDataAsync(DataRequest<Item> request, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(new DataResponse<Item>([.. _items], _items.Count));
		}
	}
}
