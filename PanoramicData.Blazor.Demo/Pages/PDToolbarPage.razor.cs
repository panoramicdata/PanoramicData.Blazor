namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDToolbarPage
{
	private string _searchText;
	private readonly ButtonSizes _size;

	private readonly List<MenuItem> _fileMenuItems =
	[
		// ctrl-n is reserved by Chrome
		new MenuItem { Text = "&&New", IconCssClass = "fas fa-fw fa-file-word", ShortcutKey = (ShortcutKey)"ctrl-alt-n" },
		new MenuItem { Key = "Open", Text = "&&Open...", IconCssClass = "fas fa-fw fa-folder-open", ShortcutKey = ShortcutKey.Create("ctrl-o") },
		new MenuItem { IsSeparator = true },
		new MenuItem { Text = "&&Save", IconCssClass = "fas fa-fw fa-save", IsDisabled = true, ShortcutKey = new ShortcutKey { Key = "s", CtrlKey = true } },
		new MenuItem { Key = "SaveAs", Text = "Save &&As...", ShortcutKey = ShortcutKey.Create("ctrl-a") },
		new MenuItem { Text = "Exit", IsVisible = false }
	];

	public PDToolbarPage()
	{
		_searchText = string.Empty;
		_size = ButtonSizes.Small;
	}

	private bool ShowButtons { get; set; } = true;
	private bool EnableButtons { get; set; } = true;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	public void OnButtonClick(KeyedEventArgs<MouseEventArgs> args) => EventManager?.Add(new Event("Click", new EventArgument("Key", args.Key)));

	private void OnEditMenuClick(string itemKey) => EventManager?.Add(new Event("EditMenuClick", new EventArgument("Key", itemKey)));

	private void OnKeypress(KeyboardEventArgs args) => EventManager?.Add(new Event("Keypress", new EventArgument("Key", args.Code)));

	private void OnFileMenuClick(string itemKey) => EventManager?.Add(new Event("FileMenuClick", new EventArgument("Key", itemKey)));

	private void OnCleared() => EventManager?.Add(new Event("Cleared"));

	private void OnValueChanged(string value)
	{
		_searchText = value;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Value", value)));
	}
}
