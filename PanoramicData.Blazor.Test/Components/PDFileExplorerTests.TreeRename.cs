using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Folder tree rename tests for <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorerTests
{
	/// <summary>Renaming is refused when the application cancels it or renaming is turned off.</summary>
	/// <param name="allowRename">The AllowRename parameter.</param>
	/// <param name="cancel">Whether the BeforeRename handler cancels.</param>
	[Theory]
	[InlineData(true, true)]
	[InlineData(false, false)]
	public async Task TreeRename_IsRefused_WhenCancelledOrDisallowed(bool allowRename, bool cancel)
	{
		RenameArgs? seen = null;
		var cut = RenderExplorer(p => p
			.Add(x => x.AllowRename, allowRename)
			.Add(x => x.BeforeRename, (RenameArgs a) => { seen = a; a.Cancel = cancel; }));
		await NavigateAsync(cut, "/Docs");
		var tree = Tree(cut);
		var args = new TreeNodeBeforeEditEventArgs<FileExplorerItem>(tree.Instance.SelectedNode!);

		await tree.InvokeAsync(() => tree.Instance.BeforeEdit.InvokeAsync(args));

		args.Cancel.Should().BeTrue();
		seen!.Item!.Path.Should().Be("/Docs");
	}

	/// <summary>A folder whose CanRename is false cannot be renamed.</summary>
	[Fact]
	public async Task TreeRename_IsRefused_WhenItemCannotBeRenamed()
	{
		_provider.Get("/Docs").CanRename = false;
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");
		var tree = Tree(cut);
		var args = new TreeNodeBeforeEditEventArgs<FileExplorerItem>(tree.Instance.SelectedNode!);

		await tree.InvokeAsync(() => tree.Instance.BeforeEdit.InvokeAsync(args));

		args.Cancel.Should().BeTrue();
	}

	/// <summary>A tree rename to a name beginning with a period is refused and reported.</summary>
	[Fact]
	public async Task TreeAfterEdit_LeadingPeriod_IsRefused()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");

		var args = await AfterTreeEditAsync(cut, "Sub", ".Sub");

		args.Cancel.Should().BeTrue();
		_exceptions.Should().ContainSingle().Which.Should().BeOfType<PDFileExplorerException>();
		_provider.Updates.Should().BeEmpty();
	}

	/// <summary>A tree rename to the name of a sibling is refused and reported.</summary>
	[Fact]
	public async Task TreeAfterEdit_SiblingName_IsRefused()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await AfterTreeEditAsync(cut, "Docs", "media");

		args.Cancel.Should().BeTrue();
		_exceptions.Should().ContainSingle().Which.Message.Should().Contain("already exists");
	}

	/// <summary>Committing the original name unchanged is refused silently.</summary>
	[Fact]
	public async Task TreeAfterEdit_OriginalName_IsRefusedSilently()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		var args = await AfterTreeEditAsync(cut, "Docs", "Docs");

		args.Cancel.Should().BeTrue();
		_exceptions.Should().BeEmpty();
	}

	/// <summary>
	/// Renaming a folder by changing only its case is not a clash with itself: the rename is sent to the provider,
	/// as the table allows for case-only renames (#174).
	/// </summary>
	[Fact]
	public async Task TreeAfterEdit_CaseOnlyChange_RenamesFolder()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");

		var args = await AfterTreeEditAsync(cut, "Sub", "SUB");

		args.Cancel.Should().BeFalse();
		_exceptions.Should().BeEmpty();
		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Docs/SUB");
		cut.Instance.FolderPath.Should().Be("/Docs/SUB");
	}

	/// <summary>A valid tree rename is sent to the provider and the folder's nodes take the new path.</summary>
	[Fact]
	public async Task TreeAfterEdit_ValidName_RenamesFolderAndDescendants()
	{
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs/Sub");
		await NavigateAsync(cut, "/Docs");

		await AfterTreeEditAsync(cut, "Docs", "Papers");

		_provider.Updates.Should().ContainSingle().Which.Delta["Path"].Should().Be("/Papers");
		Tree(cut).Instance.RootNode.Find("/Papers").Should().NotBeNull();
		Tree(cut).Instance.RootNode.Find("/Papers/Sub")!.Data!.Path.Should().Be("/Papers/Sub");
		cut.Instance.FolderPath.Should().Be("/Papers");
	}

	/// <summary>A tree rename the provider refuses leaves the folder as it was.</summary>
	[Fact]
	public async Task TreeAfterEdit_ProviderRefuses_LeavesFolder()
	{
		_provider.RefuseUpdates();
		var cut = RenderExplorer();
		await NavigateAsync(cut, "/Docs");

		await AfterTreeEditAsync(cut, "Docs", "Papers");

		Tree(cut).Instance.RootNode.Find("/Docs").Should().NotBeNull();
		cut.Instance.FolderPath.Should().Be("/Docs");
	}

	private static async Task<TreeNodeAfterEditEventArgs<FileExplorerItem>> AfterTreeEditAsync(IRenderedComponent<PDFileExplorer> cut, string oldName, string newName)
	{
		var tree = Tree(cut);
		var args = new TreeNodeAfterEditEventArgs<FileExplorerItem>(tree.Instance.SelectedNode!, oldName, newName);
		await tree.InvokeAsync(() => tree.Instance.AfterEdit.InvokeAsync(args));
		return args;
	}
}
