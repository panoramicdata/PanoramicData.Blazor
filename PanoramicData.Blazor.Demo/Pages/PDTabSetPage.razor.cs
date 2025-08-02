namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTabSetPage
{
	private readonly List<TabInfo> _tabs =
	[
		new TabInfo { Title = "Tab 1", Content = "This is the first tab." },
		new TabInfo { Title = "Tab 2", Content = "This is the second tab." }
	];

	private int _tabCounter = 3;
	private string _eventLog = string.Empty;

	private void OnTabSelected(PDTab tab)
	{
		LogEvent($"Selected: {tab.Title}");
	}

	private void OnTabClosed(PDTab tab)
	{
		var tabInfo = _tabs.FirstOrDefault(t => t.Id == tab.Id);
		if (tabInfo != null)
		{
			_tabs.Remove(tabInfo);
			LogEvent($"Closed: {tab.Title}");
		}
	}

	private void OnTabAdded()
	{
		var newTab = new TabInfo
		{
			Title = $"Tab {_tabCounter++}",
			Content = $"This is tab {_tabCounter - 1}."
		};
		_tabs.Add(newTab);
		LogEvent($"Added: {newTab.Title}");
	}

	private void OnTabRenamed(PDTab tab)
	{
		var tabInfo = _tabs.FirstOrDefault(t => t.Id == tab.Id);
		if (tabInfo != null)
		{
			tabInfo.Title = tab.Title;
			LogEvent($"Renamed: {tab.Title}");
		}
	}

	private void LogEvent(string message)
	{
		_eventLog = $"{DateTime.Now:T}: {message}\n{_eventLog}";
	}

	private class TabInfo
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Title { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
	}
}
