using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Node expansion tests for <see cref="PDStatusRollUp"/>: expansion callbacks and patching the updated nodes into the tree.
/// </summary>
public partial class PDStatusRollUpTests
{
	/// <summary>
	/// Verifies that without an expansion callback there is never anything to update.
	/// </summary>
	[Fact]
	public async Task ExpandNode_WithoutACallback_ReturnsNull()
	{
		var component = Render<PDStatusRollUp>(parameters => parameters.Add(p => p.Node, Tree()));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().BeNull();
	}

	/// <summary>
	/// Verifies that with no node there is nothing to expand, even with a callback.
	/// </summary>
	[Fact]
	public async Task ExpandNode_WithoutANode_ReturnsNull()
	{
		var calls = 0;
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.OnBeforeExpand, node => { calls++; return Task.FromResult<PDStatusRollUpNode?>(node); }));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().BeNull();
		calls.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a path that does not lead to a node is ignored without calling the callback.
	/// </summary>
	[Theory]
	[InlineData("5")]
	[InlineData("-1")]
	[InlineData("x")]
	[InlineData("1.3")]
	[InlineData("0.0")]
	public async Task ExpandNode_ForAPathThatLeadsNowhere_ReturnsNull(string path)
	{
		var calls = 0;
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, Tree())
			.Add(p => p.OnBeforeExpand, node => { calls++; return Task.FromResult<PDStatusRollUpNode?>(node); }));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(path));

		result.Should().BeNull();
		calls.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a callback declining to update leaves the node unchanged and returns nothing.
	/// </summary>
	[Fact]
	public async Task ExpandNode_WhenTheCallbackDeclines_LeavesTheNode()
	{
		var tree = Tree();
		PDStatusRollUpNode? received = null;
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, tree)
			.Add(p => p.OnBeforeExpand, node => { received = node; return Task.FromResult<PDStatusRollUpNode?>(null); }));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("1"));

		result.Should().BeNull();
		received.Should().BeSameAs(tree.Children[1]);
		tree.Children[1].Title.Should().Be("Child 1");
	}

	/// <summary>
	/// Verifies that updating the root copies the update onto the node, re-renders the trigger and returns the update as JSON.
	/// </summary>
	[Fact]
	public async Task ExpandNode_ForTheRoot_UpdatesTheTrigger()
	{
		var tree = Tree();
		var update = new PDStatusRollUpNode { Status = RollUpStatus.Red, Title = "Root", Summary = "Down", Detail = "Timed out" };
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, tree)
			.Add(p => p.OnBeforeExpand, _ => Task.FromResult<PDStatusRollUpNode?>(update)));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().Contain("\"status\":\"red\"").And.Contain("\"detail\":\"Timed out\"");
		tree.Status.Should().Be(RollUpStatus.Red);
		tree.Summary.Should().Be("Down");
		tree.Detail.Should().Be("Timed out");
		tree.Children.Should().BeEmpty();
		component.Find(".pdsr-trigger i").ClassList.Should().Contain("text-danger");
	}

	/// <summary>
	/// Verifies that updating a descendant replaces it in the tree, so a later expansion sees the update.
	/// </summary>
	[Fact]
	public async Task ExpandNode_ForADescendant_ReplacesItInTheTree()
	{
		var tree = Tree();
		var update = new PDStatusRollUpNode { Status = RollUpStatus.Green, Title = "Recovered" };
		var received = new List<PDStatusRollUpNode>();
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, tree)
			.Add(p => p.OnBeforeExpand, node => { received.Add(node); return Task.FromResult<PDStatusRollUpNode?>(update); }));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("1.0"));
		await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("1.0"));

		result.Should().Contain("\"title\":\"Recovered\"");
		tree.Children[1].Children[0].Should().BeSameAs(update);
		received[0].Title.Should().Be("Grandchild 1.0");
		received[1].Should().BeSameAs(update);
		tree.Status.Should().Be(RollUpStatus.Amber);
	}

	/// <summary>
	/// Verifies that an update is not patched in when the callback itself removed the node it was asked about,
	/// rather than being written to a position that now holds something else.
	/// </summary>
	[Theory]
	[InlineData("0")]
	[InlineData("1.0")]
	public async Task ExpandNode_ForANodeThatVanished_DoesNotPatchTheTree(string path)
	{
		var tree = Tree();
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, tree)
			.Add(p => p.OnBeforeExpand, _ =>
			{
				tree.Children.Clear();
				return Task.FromResult<PDStatusRollUpNode?>(new PDStatusRollUpNode { Title = "Late" });
			}));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(path));

		result.Should().Contain("\"title\":\"Late\"");
		tree.Children.Should().BeEmpty();
	}
}
