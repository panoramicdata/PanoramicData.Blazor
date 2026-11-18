using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// How <see cref="PDTree{TItem}"/> contains failures: given an ExceptionHandler, a failure in a handler is passed to it
/// rather than ending the Blazor circuit, and without one it is thrown as before.
/// </summary>
public partial class PDTreeTests
{
	private static readonly TimeSpan _clickPatience = TimeSpan.FromSeconds(5);

	private static void FailToSelectB(TreeNode<Item> node)
	{
		if (node.Key == "b")
		{
			throw new InvalidOperationException($"Cannot select {node.Key}");
		}
	}

	private IRenderedComponent<PDTree<Item>> RenderFailingTree(bool withExceptionHandler)
	{
		var tree = Render<PDTree<Item>>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, _provider)
				.Add(p => p.KeyField, item => item.Id)
				.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty)
				.Add(p => p.TextField, item => item.Name)
				.Add(p => p.AllowSelection, true)
				.Add(p => p.SelectionChange, node => FailToSelectB(node));
			if (withExceptionHandler)
			{
				parameters.Add(p => p.ExceptionHandler, _exceptions.Add);
			}
		});
		tree.WaitForAssertion(() => tree.Instance.RootNode.Nodes.Should().NotBeNull());
		return tree;
	}

	private static Task ArrowDownAsync(IRenderedComponent<PDTree<Item>> tree)
		=> tree.Find("div.pdtree").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Code = "ArrowDown", Key = "ArrowDown" });

	/// <summary>A click, which is handled after a short wait to tell it from a double click, passes a failure on.</summary>
	[Fact]
	public async Task AClick_WhoseSelectionHandlerFails_IsPassedToTheExceptionHandler()
	{
		var tree = RenderFailingTree(withExceptionHandler: true);

		await tree.InvokeAsync(() => tree.Instance.NodeMouseDown(Node(tree, "b"), new MouseEventArgs { Button = 0 }));

		tree.WaitForAssertion(() => _exceptions.Should().ContainSingle().Which.Message.Should().Be("Cannot select b"), _clickPatience);
		tree.Instance.SelectedNode!.Key.Should().Be("b");
	}

	/// <summary>A key press whose selection handler fails passes the failure on.</summary>
	[Fact]
	public async Task AKeyPress_WhoseSelectionHandlerFails_IsPassedToTheExceptionHandler()
	{
		var tree = RenderFailingTree(withExceptionHandler: true);
		await SelectAsync(tree, "a");

		await ArrowDownAsync(tree);

		_exceptions.Should().ContainSingle().Which.Message.Should().Be("Cannot select b");
		tree.Instance.SelectedNode!.Key.Should().Be("b");
	}

	/// <summary>Without an ExceptionHandler a failing handler is thrown, as it always was.</summary>
	[Fact]
	public async Task WithoutAnExceptionHandler_AFailingHandler_IsThrownAsBefore()
	{
		var tree = RenderFailingTree(withExceptionHandler: false);
		await SelectAsync(tree, "a");

		var act = () => ArrowDownAsync(tree);

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot select b");
	}
}
