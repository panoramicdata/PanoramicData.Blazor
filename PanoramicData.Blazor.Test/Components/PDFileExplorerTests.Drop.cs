using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Drag and drop move and copy tests for <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorerTests
{
	private static async Task DropAsync(IRenderedComponent<PDFileExplorer> cut, object? target, object? payload, bool ctrl)
	{
		var table = Table(cut);
		await table.InvokeAsync(() => table.Instance.Drop.InvokeAsync(new DropEventArgs(target, payload, ctrl)));
	}

	/// <summary>Dropping files onto a folder row moves them, or copies with Ctrl held.</summary>
	[Fact]
	public async Task Drop_FilesOntoFolder_MovesOrCopies()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var rows = Table(cut).Instance.ItemsToDisplay;

		await DropAsync(cut, rows.Single(x => x.Name == "Sub"), new List<FileExplorerItem> { rows.Single(x => x.Name == "a.docx") }, ctrl: false);
		await DropAsync(cut, rows.Single(x => x.Name == "Sub"), rows.Single(x => x.Name == "b.xlsx"), ctrl: true);

		_provider.Updates.Select(x => (x.Delta["Path"], x.Delta["Copy"])).Should().Equal(("/Docs/Sub/a.docx", false), ("/Docs/Sub/b.xlsx", true));
	}

	/// <summary>A drop with no target goes to the current folder; a tree node target is unwrapped to its folder.</summary>
	[Fact]
	public async Task Drop_NoTargetOrTreeNodeTarget_IsResolved()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var file = Table(cut).Instance.ItemsToDisplay.Single(x => x.Name == "a.docx");

		await DropAsync(cut, Node(cut, "/Empty"), file, ctrl: true);
		await NavigateAsync(cut, "/Docs/Sub");
		await DropAsync(cut, null, file, ctrl: true);

		_provider.Updates.Select(x => x.Delta["Path"]).Should().Equal("/Empty/a.docx", "/Docs/Sub/a.docx");
	}

	/// <summary>Drops onto a file, onto the item itself or its own sub-folder, or of items that cannot move, are ignored.</summary>
	[Fact]
	public async Task Drop_InvalidTargetsAndPayloads_AreIgnored()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var rows = Table(cut).Instance.ItemsToDisplay;
		var sub = rows.Single(x => x.Name == "Sub");
		var docs = _provider.Get("/Docs");

		await DropAsync(cut, rows.Single(x => x.Name == "a.docx"), rows.Single(x => x.Name == "b.xlsx"), ctrl: false);
		await DropAsync(cut, sub, sub, ctrl: false);
		await DropAsync(cut, sub, docs, ctrl: false);
		await DropAsync(cut, sub, rows.Single(x => x.Name == ".."), ctrl: false);
		await DropAsync(cut, sub, "not an item", ctrl: false);

		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>
	/// A drop into a folder that cannot accept items is ignored, whether it lands on the folder's row or on the
	/// table whitespace while that folder is current (#174).
	/// </summary>
	[Fact]
	public async Task Drop_IntoReadOnlyFolder_IsIgnored()
	{
		// a different file for each drop, so that a drop wrongly accepted cannot raise a conflict prompt
		_provider.Items.Add(FileProvider.File("/Docs/c.txt", 1));
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var rows = Table(cut).Instance.ItemsToDisplay;
		var (a, b, c) = (rows.Single(x => x.Name == "a.docx"), rows.Single(x => x.Name == "b.xlsx"), rows.Single(x => x.Name == "c.txt"));
		await NavigateAsync(cut, "/");
		var media = Table(cut).Instance.ItemsToDisplay.Single(x => x.Name == "Media");

		await DropAsync(cut, media, a, ctrl: false);
		await DropAsync(cut, Node(cut, "/Media"), b, ctrl: true);
		await NavigateAsync(cut, "/Media");
		await DropAsync(cut, null, c, ctrl: true);

		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>
	/// The ".." row is read-only itself, but a drop on it still moves the items into the parent folder it stands for
	/// when that folder accepts items.
	/// </summary>
	[Fact]
	public async Task Drop_OntoParentFolderRow_MovesIntoParent()
	{
		_provider.Items.Add(FileProvider.File("/Docs/Sub/d.txt", 1));
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		var rows = Table(cut).Instance.ItemsToDisplay;

		await DropAsync(cut, rows.Single(x => x.Name == ".."), rows.Single(x => x.Name == "d.txt"), ctrl: false);

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Docs/d.txt");
	}

	/// <summary>Dropping the current folder somewhere else moves it and follows it to its target.</summary>
	[Fact]
	public async Task Drop_CurrentFolder_MovesIt_AndSelectsTarget()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		var tree = Tree(cut);
		var current = tree.Instance.SelectedNode!.Data!;

		await tree.InvokeAsync(() => tree.Instance.Drop.InvokeAsync(new DropEventArgs(Node(cut, "/Empty"), current, false)));

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Empty/Sub");
		cut.Instance.FolderPath.Should().Be("/Empty");
	}
}
