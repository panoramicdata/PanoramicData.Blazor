namespace PanoramicData.Blazor;

/// <summary>
/// Specifies where the create-tab button is rendered within a <see cref="PDTabSet"/>.
/// </summary>
public enum CreateTabPosition
{
	/// <summary>Render the create-tab button at the start (left) of the tab strip.</summary>
	Start,
	/// <summary>Render the create-tab button at the end (right) of the tab strip.</summary>
	End,
	/// <summary>Render the create-tab button at both ends of the tab strip.</summary>
	Both
}

/// <summary>
/// A Blazor component that provides a tabbed container supporting dynamic tab management.
/// </summary>
public partial class PDTabSet : ComponentBase
{
	internal List<PDTab> Tabs { get; } = [];
	internal PDTab? ActiveTab { get; set; }

	/// <summary>
	/// Gets or sets the child content of the component.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the component.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether tabs can be closed.
	/// </summary>
	[Parameter] public bool IsTabClosingEnabled { get; set; } = false;

	/// <summary>
	/// Gets or sets the minimum width of a tab.
	/// </summary>
	[Parameter] public string TabMinWidth { get; set; } = "100px";

	/// <summary>
	/// Gets or sets the maximum width of a tab.
	/// </summary>
	[Parameter] public string TabMaxWidth { get; set; } = "240px";

	/// <summary>
	/// Gets or sets whether new tabs can be added.
	/// </summary>
	[Parameter] public bool IsTabAdditionEnabled { get; set; } = false;

	/// <summary>
	/// Gets or sets the position of the create tab button.
	/// </summary>
	[Parameter] public CreateTabPosition CreateTabPosition { get; set; } = CreateTabPosition.End;

	/// <summary>
	/// Gets or sets whether tabs can be renamed.
	/// </summary>
	[Parameter] public bool IsTabRenamingEnabled { get; set; } = false;

	/// <summary>
	/// An event callback that is invoked when a tab is selected.
	/// </summary>
	[Parameter] public EventCallback<PDTab> OnTabSelected { get; set; }

	/// <summary>
	/// An event callback that is invoked when a tab is closed.
	/// </summary>
	[Parameter] public EventCallback<PDTab> OnTabClosed { get; set; }

	/// <summary>
	/// An event callback that is invoked when a new tab is added.
	/// </summary>
	[Parameter] public EventCallback OnTabAdded { get; set; }

	/// <summary>
	/// An event callback that is invoked when a tab is renamed.
	/// </summary>
	[Parameter] public EventCallback<PDTab> OnTabRenamed { get; set; }

	internal void AddTab(PDTab tab)
	{
		Tabs.Add(tab);
		ActiveTab ??= tab;

		StateHasChanged();
	}

	internal void SelectTab(PDTab tab)
	{
		ActiveTab = tab;
		if (OnTabSelected.HasDelegate)
		{
			OnTabSelected.InvokeAsync(tab);
		}

		tab.OnSelected.InvokeAsync();
		StateHasChanged();
	}

	internal void CloseTab(PDTab tab, MouseEventArgs? e = null)
	{
		if (Tabs.Remove(tab))
		{
			if (ActiveTab == tab)
			{
				ActiveTab = Tabs.FirstOrDefault();
			}

			if (OnTabClosed.HasDelegate)
			{
				OnTabClosed.InvokeAsync(tab);
			}

			StateHasChanged();
		}
	}

	internal bool GetTabCanBeClosed(PDTab tab)
		=> tab.IsClosingEnabled ?? IsTabClosingEnabled;

	internal bool GetTabCanBeRenamed(PDTab tab)
		=> tab.IsRenamingEnabled ?? IsTabRenamingEnabled;

	internal void OnAddTabClicked()
	{
		if (OnTabAdded.HasDelegate)
		{
			OnTabAdded.InvokeAsync();
		}
	}

	internal void StartRenamingTab(PDTab tab)
	{
		if (GetTabCanBeRenamed(tab))
		{
			tab.IsRenaming = true;
			StateHasChanged();
		}
	}

	internal static void OnRenameTabInput(PDTab tab, ChangeEventArgs e)
	{
		tab.TempTitle = e.Value?.ToString() ?? string.Empty;
	}

	internal void OnRenameTabBlur(PDTab tab, FocusEventArgs e)
	{
		CommitTabRename(tab);
	}

	internal void OnRenameTabKeyDown(PDTab tab, KeyboardEventArgs e)
	{
		if (e.Key == "Enter")
		{
			CommitTabRename(tab);
		}
		else if (e.Key == "Escape")
		{
			tab.IsRenaming = false;
			tab.TempTitle = tab.Title;
			StateHasChanged();
		}
	}

	private void CommitTabRename(PDTab tab)
	{
		if (tab.TempTitle != tab.Title)
		{
#pragma warning disable BL0005 // Parent component manages tab title during rename
			tab.Title = tab.TempTitle;
#pragma warning restore BL0005

			if (OnTabRenamed.HasDelegate)
			{
				OnTabRenamed.InvokeAsync(tab);
			}
		}

		tab.IsRenaming = false;

		StateHasChanged();
	}
}
