namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTabSetPage
{
	private readonly List<TabInfo> _tabs =
	[
		new TabInfo { Title = "Overview",  IconCssClass = "fas fa-home",      Content = "This is the overview tab." },
		new TabInfo { Title = "Settings",  IconCssClass = "fas fa-cog",       Content = "This is the settings tab." },
		new TabInfo { Title = "Reports",   IconCssClass = "fas fa-chart-bar", Content = "This is the reports tab." },
	];

	private int _tabCounter = 4;
	private string _eventLog = string.Empty;

	// Control-panel bindings
	private bool _isReorderingEnabled = true;
	private bool _isClosingEnabled    = true;
	private bool _isRenamingEnabled   = true;
	private bool _isAddingEnabled     = true;
	private string _tabMinWidth = "100px";
	private string _tabMaxWidth = "200px";

	private void AddTab()
	{
		var newTab = new TabInfo
		{
			Title   = $"Tab {_tabCounter++}",
			Content = $"Content for tab {_tabCounter - 1}."
		};
		_tabs.Add(newTab);
		LogEvent($"Added: {newTab.Title}");
	}

	private void OnTabSelected(PDTab tab) => LogEvent($"Selected: {tab.Title}");

	private void OnTabClosed(PDTab tab)
	{
		var tabInfo = _tabs.FirstOrDefault(t => t.Id == tab.Id);
		if (tabInfo != null)
		{
			_tabs.Remove(tabInfo);
			LogEvent($"Closed: {tab.Title}");
		}
	}

	private void OnTabAdded(CreateTabPosition position)
	{
		var newTab = new TabInfo
		{
			Title   = $"Tab {_tabCounter++}",
			Content = $"Content for tab {_tabCounter - 1}."
		};

		if (position == CreateTabPosition.Start)
		{
			_tabs.Insert(0, newTab);
		}
		else
		{
			_tabs.Add(newTab);
		}

		LogEvent($"Added: {newTab.Title} ({position})");
	}

	private void OnTabRenamed(PDTab tab)
	{
		var tabInfo = _tabs.FirstOrDefault(t => t.Id == tab.Id);
		if (tabInfo != null)
		{
			tabInfo.Title = tab.Title;
			LogEvent($"Renamed to: {tab.Title}");
		}
	}

	private void OnTabsReordered(IReadOnlyList<PDTab> tabs)
	{
		// Mirror the new order back into _tabs so the controls panel stays in sync.
		var reordered = tabs
			.Select(t => _tabs.FirstOrDefault(ti => ti.Id == t.Id))
			.OfType<TabInfo>()
			.ToList();

		_tabs.Clear();
		_tabs.AddRange(reordered);
		LogEvent("Tabs reordered: " + string.Join(", ", reordered.Select(t => t.Title)));
	}

	private void LogEvent(string message)
	{
		_eventLog = $"{DateTime.Now:T}: {message}\n{_eventLog}";
	}

	private sealed class TabInfo
	{
		public Guid   Id          { get; init; } = Guid.NewGuid();
		public string Title       { get; set;  } = string.Empty;
		public string Content     { get; set;  } = string.Empty;
		public string? IconCssClass { get; init; }
	}
}
