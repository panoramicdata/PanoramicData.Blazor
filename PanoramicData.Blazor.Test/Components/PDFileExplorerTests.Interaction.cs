using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Helpers that drive a <see cref="PDFileExplorer"/> as a user does: navigating, selecting rows, clicking toolbar
/// buttons and menu entries, answering dialogs and pressing keys.
/// </summary>
public partial class PDFileExplorerTests : BunitContext
{
	private static async Task NavigateAsync(IRenderedComponent<PDFileExplorer> cut, string path)
		=> await cut.InvokeAsync(() => cut.Instance.NavigateToAsync(path));

	private static async Task SelectRowsAsync(IRenderedComponent<PDFileExplorer> cut, params string[] paths)
	{
		var table = Table(cut);
		for (var i = 0; i < paths.Length; i++)
		{
			var path = paths[i];
			var ctrl = i > 0;
			await table.InvokeAsync(() => table.Instance.SelectItemAsync(path, false, ctrl));
		}
	}

	private static async Task ClickToolbarAsync(IRenderedComponent<PDFileExplorer> cut, string key)
	{
		var toolbar = cut.FindComponents<PDToolbar>().First(t => t.Instance.Items == cut.Instance.ToolbarItems);
		await toolbar.InvokeAsync(() => toolbar.Instance.ButtonClick.InvokeAsync(new KeyedEventArgs<MouseEventArgs>(key, new MouseEventArgs())));
	}

	private static async Task AnswerAsync(IRenderedComponent<PDModal> modal, string key)
		=> await modal.InvokeAsync(() => modal.Instance.OnButtonClick(new KeyedEventArgs<MouseEventArgs>(key, new MouseEventArgs())));

	private static async Task KeyDownTableAsync(IRenderedComponent<PDFileExplorer> cut, string code, bool ctrl = false)
	{
		var table = Table(cut);
		await table.InvokeAsync(() => table.Instance.KeyDown.InvokeAsync(new KeyboardEventArgs { Code = code, CtrlKey = ctrl }));
	}

	private static async Task KeyDownTreeAsync(IRenderedComponent<PDFileExplorer> cut, string code, bool ctrl = false)
	{
		var tree = Tree(cut);
		await tree.InvokeAsync(() => tree.Instance.KeyDown.InvokeAsync(new KeyboardEventArgs { Code = code, CtrlKey = ctrl }));
	}

	private static MenuItem MenuEntry(IRenderedComponent<PDContextMenu> menu, string text)
		=> menu.Instance.Items.First(x => x.Text == text);

	private static async Task UpdateMenuAsync(IRenderedComponent<PDContextMenu> menu, ElementInfo? source = null)
	{
		var args = new MenuItemsEventArgs(menu.Instance, menu.Instance.Items) { SourceElement = source };
		await menu.InvokeAsync(() => menu.Instance.UpdateState.InvokeAsync(args));
	}

	private static async Task ClickMenuAsync(IRenderedComponent<PDContextMenu> menu, string text)
	{
		var item = MenuEntry(menu, text);
		await menu.InvokeAsync(() => menu.Instance.ClickHandler(item));
	}

	private static string[] VisibleMenuTexts(IRenderedComponent<PDContextMenu> menu)
		=> [.. menu.Instance.Items.Where(x => x.IsVisible && !x.IsSeparator).Select(x => x.Text)];
}
