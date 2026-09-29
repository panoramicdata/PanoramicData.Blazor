using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Node editing tests for <see cref="PDTree{TItem}"/>: beginning, committing and cancelling an edit.
/// </summary>
public partial class PDTreeTests
{
	/// <summary>Selecting the selected node again with auto edit begins an edit when editing is allowed.</summary>
	[Fact]
	public async Task SelectingTheSelectedNodeAgain_BeginsAnEdit()
	{
		var tree = RenderItemTree(p => p.Add(x => x.AllowEdit, true));
		await SelectAsync(tree, "a");

		await tree.InvokeAsync(() => tree.Instance.SelectNode(Node(tree, "a")));

		Node(tree, "a").IsEditing.Should().BeTrue();
		tree.Find("input.pdtreenode_edit").GetAttribute("value").Should().Be("Alpha");
		_selections.Should().ContainSingle();
	}

	/// <summary>BeginEdit is refused when editing is not allowed, and BeforeEdit can cancel it.</summary>
	[Fact]
	public async Task BeginEdit_HonoursAllowEditAndBeforeEdit()
	{
		var readOnly = RenderItemTree();
		await SelectAsync(readOnly, "a");
		await readOnly.InvokeAsync(readOnly.Instance.BeginEdit);
		Node(readOnly, "a").IsEditing.Should().BeFalse();

		var cancelling = RenderItemTree(p => p
			.Add(x => x.AllowEdit, true)
			.Add(x => x.BeforeEdit, (TreeNodeBeforeEditEventArgs<Item> e) => e.Cancel = true));
		await SelectAsync(cancelling, "a");
		await cancelling.InvokeAsync(cancelling.Instance.BeginEdit);
		Node(cancelling, "a").IsEditing.Should().BeFalse();
	}

	private async Task<IRenderedComponent<PDTree<Item>>> EditAsync(string key, string newText, Action<TreeNodeAfterEditEventArgs<Item>>? afterEdit = null)
	{
		var tree = RenderItemTree(p =>
		{
			p.Add(x => x.AllowEdit, true);
			if (afterEdit != null)
			{
				p.Add(x => x.AfterEdit, afterEdit);
			}
		});
		await SelectAsync(tree, key);
		await tree.InvokeAsync(tree.Instance.BeginEdit);
		tree.Find("input.pdtreenode_edit").Input(newText);
		return tree;
	}

	/// <summary>Committing an edit renames the node, re-sorts its siblings and reports the old and new text.</summary>
	[Fact]
	public async Task CommitEdit_RenamesAndResorts()
	{
		TreeNodeAfterEditEventArgs<Item>? args = null;
		var tree = await EditAsync("a", "Zulu", e => args = e);

		await tree.InvokeAsync(tree.Instance.CommitEdit);

		Node(tree, "a").Text.Should().Be("Zulu");
		Node(tree, "a").IsEditing.Should().BeFalse();
		tree.Instance.RootNode.Nodes!.Select(n => n.Key).Should().Equal("b", "a");
		args!.OldValue.Should().Be("Alpha");
		args.NewValue.Should().Be("Zulu");
	}

	/// <summary>AfterEdit may change the committed text, or cancel the edit.</summary>
	[Fact]
	public async Task AfterEdit_CanAlterOrCancel()
	{
		var altered = await EditAsync("a", "Zulu", e => e.NewValue = "Yankee");
		await altered.InvokeAsync(altered.Instance.CommitEdit);
		Node(altered, "a").Text.Should().Be("Yankee");

		var cancelled = await EditAsync("a", "Zulu", e => e.Cancel = true);
		await cancelled.InvokeAsync(cancelled.Instance.CommitEdit);
		Node(cancelled, "a").Text.Should().Be("Alpha");
		Node(cancelled, "a").IsEditing.Should().BeFalse();
	}

	/// <summary>Committing blank text cancels the edit and reports that a value is required.</summary>
	[Fact]
	public async Task CommitEdit_WithBlankText_IsRefused()
	{
		var tree = await EditAsync("a", "   ");

		await tree.InvokeAsync(tree.Instance.CommitEdit);

		Node(tree, "a").Text.Should().Be("Alpha");
		_exceptions.Should().ContainSingle().Which.Message.Should().Be("A value is required");
	}

	/// <summary>CancelEdit abandons the typed text.</summary>
	[Fact]
	public async Task CancelEdit_AbandonsTheText()
	{
		var tree = await EditAsync("a", "Zulu");

		await tree.InvokeAsync(tree.Instance.CancelEdit);

		Node(tree, "a").Text.Should().Be("Alpha");
		Node(tree, "a").IsEditing.Should().BeFalse();
	}

	/// <summary>Escape cancels an edit and Enter commits one, and every key is passed on through KeyDown.</summary>
	[Fact]
	public async Task EscapeAndEnter_CancelAndCommit()
	{
		var keys = new List<string>();
		var escaped = await EditAsync("a", "Zulu");
		escaped.Find("div.pdtree").KeyDown(new KeyboardEventArgs { Code = "Escape" });
		Node(escaped, "a").Text.Should().Be("Alpha");

		var entered = await EditAsync("b", "Echo");
		entered.Render(p => p.Add(x => x.KeyDown, (KeyboardEventArgs e) => keys.Add(e.Code)));
		Key(entered, "Enter");
		Node(entered, "b").Text.Should().Be("Echo");
		keys.Should().Equal("Enter");
	}

	/// <summary>Leaving the edit box commits the edit.</summary>
	[Fact]
	public async Task BlurringTheEditBox_CommitsTheEdit()
	{
		var tree = await EditAsync("a", "Zulu");

		tree.Find("input.pdtreenode_edit").Blur();

		Node(tree, "a").Text.Should().Be("Zulu");
	}

	/// <summary>F2 begins an edit of the selected node.</summary>
	[Fact]
	public async Task F2_BeginsAnEdit()
	{
		var tree = RenderItemTree(p => p.Add(x => x.AllowEdit, true));
		await SelectAsync(tree, "b");

		Key(tree, "F2");

		Node(tree, "b").IsEditing.Should().BeTrue();
	}
}
