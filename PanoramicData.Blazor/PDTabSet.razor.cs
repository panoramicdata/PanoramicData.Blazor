namespace PanoramicData.Blazor;

public enum CreateTabPosition
{
	Start,
	End,
	Both
}

public partial class PDTabSet : ComponentBase
{
	internal List<PDTab> Tabs { get; } = [];
	internal PDTab? ActiveTab { get; set; }

	[Parameter] public RenderFragment? ChildContent { get; set; }
	[Parameter] public string CssClass { get; set; } = string.Empty;
	[Parameter] public bool IsTabClosingEnabled { get; set; } = false;
	[Parameter] public string TabMinWidth { get; set; } = "100px";
	[Parameter] public string TabMaxWidth { get; set; } = "240px";
	[Parameter] public bool IsTabAdditionEnabled { get; set; } = false;
	[Parameter] public CreateTabPosition CreateTabPosition { get; set; } = CreateTabPosition.End;
	[Parameter] public bool IsTabRenamingEnabled { get; set; } = false;
	[Parameter] public EventCallback<PDTab> OnTabSelected { get; set; }
	[Parameter] public EventCallback<PDTab> OnTabClosed { get; set; }
	[Parameter] public EventCallback OnTabAdded { get; set; }
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
		if (Tabs.Contains(tab))
		{
			Tabs.Remove(tab);
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
			tab.Title = tab.TempTitle;

			if (OnTabRenamed.HasDelegate)
			{
				OnTabRenamed.InvokeAsync(tab);
			}
		}

		tab.IsRenaming = false;

		StateHasChanged();
	}
}
