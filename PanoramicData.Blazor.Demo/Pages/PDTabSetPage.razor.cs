namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTabSetPage
{
	private readonly List<TabInfo> _tabs =
	[
		new TabInfo { Title = "Overview",  IconCssClass = "fas fa-home",      Content = "This is the overview tab." },
		new TabInfo { Title = "Settings",  IconCssClass = "fas fa-cog",       Content = "This is the settings tab." },
		new TabInfo { Title = "Reports",   IconCssClass = "fas fa-chart-bar", Content = "This is the reports tab." },
	];

	// Fixed ids so a query-string value can be mapped to a tab. The PDTabSet itself never touches the URL.
	private static readonly (string Name, Guid Id)[] _urlTabs =
	[
		("Overview", new Guid("7d1f3b52-0c4e-4a38-9b6a-1e2f5c8d0a01")),
		("Settings", new Guid("7d1f3b52-0c4e-4a38-9b6a-1e2f5c8d0a02")),
		("Reports", new Guid("7d1f3b52-0c4e-4a38-9b6a-1e2f5c8d0a03")),
	];

	private int _tabCounter = 4;
	private string _eventLog = string.Empty;

	[Inject] private NavigationManager NavigationManager { get; set; } = default!;

	/// <summary>The <c>section</c> query-string value, for example <c>reports</c>. (<c>tab</c> is used by the demo shell.)</summary>
	[SupplyParameterFromQuery(Name = "section")]
	public string? Section { get; set; }

	// The tab named in the URL, or null (leave the first tab selected) when absent or unrecognised.
	private Guid? UrlSelectedTabId => _urlTabs
		.Where(t => string.Equals(t.Name, Section, StringComparison.OrdinalIgnoreCase))
		.Select(t => (Guid?)t.Id)
		.FirstOrDefault();

	private void OnUrlSelectedTabIdChanged(Guid? id)
	{
		var name = _urlTabs.FirstOrDefault(t => t.Id == id).Name;
		if (name is null)
		{
			return;
		}

		// replace: true so clicking through tabs does not fill the browser history.
		NavigationManager.NavigateTo(
			NavigationManager.GetUriWithQueryParameter("section", name.ToLowerInvariant()),
			replace: true);
		LogEvent($"URL updated: ?section={name.ToLowerInvariant()}");
	}

	// Control-panel bindings
	protected bool IsReorderingEnabled { get; set; } = true;
	protected bool IsClosingEnabled { get; set; }    = true;
	protected bool IsRenamingEnabled { get; set; }   = true;
	protected bool IsAddingEnabled { get; set; }     = true;
	protected string TabMinWidth { get; set; } = "100px";
	protected string TabMaxWidth { get; set; } = "200px";

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
