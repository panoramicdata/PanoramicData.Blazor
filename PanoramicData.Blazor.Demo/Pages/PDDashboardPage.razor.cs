using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDashboardPage
{
	[CascadingParameter] protected EventManager? EventManager { get; set; }

	protected bool DisplayMode { get; set; }
	protected bool ShowName { get; set; } = true;
	protected DisplayModeHeaderContent DisplayModeHeader { get; set; } = DisplayModeHeaderContent.Both;
	protected bool IsRotationEnabled { get; set; }
	protected int RotationIntervalSeconds { get; set; } = 5;
	protected bool ShowMaximize { get; set; }
	protected bool ClockShowMaximize { get; set; }
	protected bool AllowViewModePropertyEdit { get; set; }
	protected PDDashboard? Dashboard { get; set; }
	private List<PDDashboardTab> _tabs = [];

	private readonly Dictionary<string, string> _dashboardProperties = new()
	{
		["theme"] = "light",
		["region"] = "us-east"
	};

	protected override void OnAfterRender(bool firstRender)
	{
		if (_clockTile is not null)
		{
			var newValue = ClockShowMaximize ? true : (bool?)null;
			if (_clockTile.ShowMaximize != newValue)
			{
				_clockTile.ShowMaximize = newValue;
				StateHasChanged();
			}
		}
	}

	protected override void OnInitialized()
	{
		_tabs =
		[
			CreateOverviewTab(),
			CreateMetricsTab()
		];
	}

	private void LogEvent(string name, params EventArgument[] args) => EventManager?.Add(new Event(name, args));

	private void OnTileMoved((PDDashboardTile Tile, int NewRow, int NewColumn) args)
		=> LogEvent("OnTileMove", new EventArgument("NewRow", args.NewRow), new EventArgument("NewColumn", args.NewColumn));

	private void OnTileResized((PDDashboardTile Tile, int NewRowSpan, int NewColumnSpan) args)
		=> LogEvent("OnTileResize", new EventArgument("NewRowSpan", args.NewRowSpan), new EventArgument("NewColumnSpan", args.NewColumnSpan));

	private void OnTabAdded(PDDashboardTab tab) => LogEvent("OnTabAdd", new EventArgument("Name", tab.Name));

	private void OnActiveTabChanged(int index) => LogEvent("ActiveTabChanged", new EventArgument("Index", index));

	private void OnTileDeleted() => LogEvent("OnTileDelete");

	private void OnEditModeChanged(bool isEditable) => LogEvent("OnEditModeChanged", new EventArgument("IsEditable", isEditable));

	private void OnSettingsChanged() => LogEvent("OnSettingsChanged");
}
