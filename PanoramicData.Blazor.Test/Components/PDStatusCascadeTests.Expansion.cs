using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Node expansion tests for <see cref="PDStatusCascade"/>: expansion callbacks and patching the updated nodes into the tree.
/// </summary>
public partial class PDStatusCascadeTests
{
	/// <summary>Verifies that an expand callback causes a callback reference to be handed to the pop-over.</summary>
	[Fact]
	public void An_expand_callback_hands_a_reference_to_the_popup()
	{
		var module = JSInterop.SetupModule(ModulePath);

		Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, Tree())
			.Add(p => p.OnBeforeExpand, node => Task.FromResult<PDStatusCascadeNode?>(null)));

		module.VerifyInvoke("init").Arguments[2].Should().BeOfType<DotNetObjectReference<PDStatusCascade>>();
	}

	/// <summary>Verifies that expanding does nothing when there is no callback.</summary>
	[Fact]
	public async Task Expanding_without_a_callback_returns_nothing()
	{
		var component = Render<PDStatusCascade>(parameters => parameters.Add(p => p.Node, Tree()));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().BeNull();
	}

	/// <summary>Verifies that expanding with no node returns nothing and does not call the callback.</summary>
	[Fact]
	public async Task Expanding_without_a_node_returns_nothing()
	{
		var calls = 0;
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.OnBeforeExpand, node =>
			{
				calls++;
				return Task.FromResult<PDStatusCascadeNode?>(node);
			}));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().BeNull();
		calls.Should().Be(0);
	}

	/// <summary>Verifies that an updated root replaces the root's content and re-renders the trigger.</summary>
	[Fact]
	public async Task An_updated_root_is_applied_and_rendered()
	{
		var root = Tree();
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, root)
			.Add(p => p.OnBeforeExpand, _ => Task.FromResult<PDStatusCascadeNode?>(
				new PDStatusCascadeNode { Status = StatusType.Red, Title = "Root now", Summary = "Down", Detail = "Detail" })));

		var json = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		json.Should().Contain("\"title\":\"Root now\"");
		root.Title.Should().Be("Root now");
		root.Summary.Should().Be("Down");
		root.Detail.Should().Be("Detail");
		root.Children.Should().BeEmpty();
		component.Find(".pdsc-trigger i").ClassList.Should().Contain("text-danger");
	}

	/// <summary>Verifies that the callback receives the node at the given path and a nested update is patched in place.</summary>
	[Fact]
	public async Task A_nested_update_is_patched_at_its_path()
	{
		var root = Tree();
		PDStatusCascadeNode? received = null;
		var replacement = new PDStatusCascadeNode { Title = "Leaf now", Status = StatusType.Amber };
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, root)
			.Add(p => p.OnBeforeExpand, node =>
			{
				received = node;
				return Task.FromResult<PDStatusCascadeNode?>(replacement);
			}));

		var json = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("1.0"));

		received!.Title.Should().Be("Leaf");
		root.Children[1].Children[0].Should().BeSameAs(replacement);
		json.Should().Contain("\"name\":\"amber\"");
	}

	/// <summary>Verifies that a path that does not name a node returns nothing and never calls the callback.</summary>
	[Theory]
	[InlineData("5")]
	[InlineData("-1")]
	[InlineData("x")]
	[InlineData("0.0")]
	public async Task An_invalid_path_returns_nothing(string path)
	{
		var calls = 0;
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, Tree())
			.Add(p => p.OnBeforeExpand, node =>
			{
				calls++;
				return Task.FromResult<PDStatusCascadeNode?>(node);
			}));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(path));

		result.Should().BeNull();
		calls.Should().Be(0);
	}

	/// <summary>Verifies that a callback returning null leaves the tree unchanged.</summary>
	[Fact]
	public async Task A_callback_returning_null_leaves_the_tree_unchanged()
	{
		var root = Tree();
		var original = root.Children[0];
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, root)
			.Add(p => p.OnBeforeExpand, _ => Task.FromResult<PDStatusCascadeNode?>(null)));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("0"));

		result.Should().BeNull();
		root.Children[0].Should().BeSameAs(original);
	}

	/// <summary>
	/// Verifies that an update is not patched in when the callback itself removed the node it was asked about,
	/// rather than being written to a position that now holds something else.
	/// </summary>
	[Theory]
	[InlineData("0")]
	[InlineData("1.0")]
	public async Task An_update_for_a_node_that_vanished_is_not_patched(string path)
	{
		var root = Tree();
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, root)
			.Add(p => p.OnBeforeExpand, _ =>
			{
				root.Children.Clear();
				return Task.FromResult<PDStatusCascadeNode?>(new PDStatusCascadeNode { Title = "Late" });
			}));

		var json = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(path));

		json.Should().Contain("Late");
		root.Children.Should().BeEmpty();
	}
}
