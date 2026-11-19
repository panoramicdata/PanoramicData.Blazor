namespace PanoramicData.Blazor;

/// <summary>
/// The output tabs and the messages shown on them.
/// </summary>
public partial class PDStudio
{
	private const string _resultsTab = "results";
	private const string _messagesTab = "messages";

	private readonly List<PDStudioMessage> _messages = [];
	private string _activeTab = _resultsTab;
	private LogLevel? _messageLevel;
	private string MessageSearch { get; set; } = string.Empty;

	/// <summary>
	/// Gets the messages recorded so far, oldest first, whether or not the Messages tab is shown.
	/// </summary>
	public IReadOnlyList<PDStudioMessage> Messages => _messages;

	// The filter starts at the options' level; Debug and below show everything.
	private LogLevel MessageLevel => _messageLevel ?? (Options.DefaultLogLevel <= LogLevel.Debug ? LogLevel.Trace : Options.DefaultLogLevel);

	private string MessageLevelValue => MessageLevel.ToString();

	private IEnumerable<PDStudioMessage> FilteredMessages => _messages.Where(m =>
		m.Level >= MessageLevel
		&& (MessageSearch.Length == 0 || m.Text.Contains(MessageSearch, StringComparison.OrdinalIgnoreCase)));

	private string MessagesBadgeCssClass => _messages.Any(m => m.Level >= LogLevel.Error)
		? "pd-studio-tab-badge pd-studio-tab-badge-error"
		: "pd-studio-tab-badge";

	// The kind sets the colour; with no text the badge is a dot
	private static string BadgeCssClass(PDStudioTabBadge badge)
		=> string.Join(' ', new[]
		{
			"pd-studio-tab-badge",
			string.IsNullOrEmpty(badge.Text) ? "pd-studio-tab-badge-dot" : null,
			badge.Kind == PDStudioTabBadgeKind.Neutral ? null : $"pd-studio-tab-badge-{badge.Kind.ToString().ToLowerInvariant()}",
			badge.CssClass
		}.Where(cssClass => !string.IsNullOrWhiteSpace(cssClass)));

	private string MessagesBadgeText
	{
		get
		{
			var errors = _messages.Count(m => m.Level >= LogLevel.Error);
			return errors > 0
				? string.Format(CultureInfo.InvariantCulture, errors == 1 ? "{0} error" : "{0} errors", errors)
				: _messages.Count.ToString(CultureInfo.InvariantCulture);
		}
	}

	// A tab that is hidden or no longer offered cannot stay selected: the first tab shown is, instead.
	private string ActiveTab => IsTabShown(_activeTab) ? _activeTab : FirstShownTab();

	/// <summary>
	/// Gets the key of the tab showing: "results", "messages", or the key of one of the <see cref="OutputTabs"/>.
	/// </summary>
	public string ActiveTabKey => ActiveTab;

	/// <summary>
	/// Raised with the key of the tab showing, once it first shows and whenever it changes, whether by a click,
	/// <see cref="SelectTab(string)"/>, or because the tab that was showing has gone. A host can use it to render a
	/// costly tab's content only while that tab shows.
	/// </summary>
	[Parameter] public EventCallback<string> ActiveTabChanged { get; set; }

	// The tab last given to ActiveTabChanged
	private string? _reportedTab;

	private bool IsTabShown(string key) => key switch
	{
		_resultsTab => Options.ShowResultsTab,
		_messagesTab => Options.IsLoggingVisible,
		_ => OutputTabs.Any(t => t.Key == key)
	};

	private string FirstShownTab()
	{
		if (Options.ShowResultsTab)
		{
			return _resultsTab;
		}

		var hostsFirstTab = OutputTabs.Count > 0 ? OutputTabs[0].Key : null;
		if (Options.IsLoggingVisible && (hostsFirstTab is null || !Options.MessagesTabLast))
		{
			return _messagesTab;
		}

		return hostsFirstTab ?? _resultsTab;
	}

	private bool IsTabActive(string tab) => tab == ActiveTab;

	private string TabCssClass(string tab) => IsTabActive(tab) ? "pd-studio-tab active" : "pd-studio-tab";

	// Text, as a bool renders the attribute bare when true and leaves it out when false
	private string AriaSelected(string tab) => IsTabActive(tab) ? "true" : "false";

	/// <summary>
	/// Shows a tab: "results", "messages", or the key of one of the <see cref="OutputTabs"/>.
	/// </summary>
	public void SelectTab(string key)
	{
		_activeTab = key;
		StateHasChanged();
	}

	/// <summary>
	/// Gets or sets tabs shown after Results and Messages, such as a query plan or a chart.
	/// </summary>
	[Parameter] public IReadOnlyList<PDStudioOutputTab> OutputTabs { get; set; } = [];

	private void OnMessageLevelChanged(ChangeEventArgs args)
		=> _messageLevel = Enum.TryParse<LogLevel>(args.Value?.ToString(), out var level) ? level : LogLevel.Trace;

	/// <summary>
	/// Removes every recorded message.
	/// </summary>
	public void ClearMessages() => _messages.Clear();

	private static string LevelCssClass(PDStudioMessage message) => message.Level switch
	{
		>= LogLevel.Error => "level-error",
		LogLevel.Warning => "level-warning",
		LogLevel.Information => message.IsSuccess ? "level-success" : "level-information",
		_ => "level-debug"
	};

	private static string LevelIcon(PDStudioMessage message) => message.Level switch
	{
		>= LogLevel.Error => "fas fa-times-circle",
		LogLevel.Warning => "fas fa-exclamation-triangle",
		LogLevel.Information => message.IsSuccess ? "fas fa-check-circle" : "fas fa-info-circle",
		_ => "fas fa-bug"
	};

	private static string LevelName(PDStudioMessage message) => message.Level switch
	{
		LogLevel.Critical => "Critical",
		LogLevel.Error => "Error",
		LogLevel.Warning => "Warning",
		LogLevel.Information => message.IsSuccess ? "Success" : "Info",
		_ => "Debug"
	};

	// Checked after each render, so a tab that changes because the one showing has gone is reported too
	private async Task ReportActiveTabAsync()
	{
		if (ActiveTab != _reportedTab)
		{
			_reportedTab = ActiveTab;
			await ActiveTabChanged.InvokeAsync(_reportedTab);
		}
	}
}
