using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDashboardPage
{
	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private bool _isEditable;
	private bool _displayMode;
	private List<PDDashboardTab> _tabs = [];

	protected override void OnInitialized()
	{
		_tabs =
		[
			new PDDashboardTab
			{
				Name = "Overview",
				Tiles =
				[
					new PDDashboardTile
					{
						RowIndex = 0, ColumnIndex = 0, ColumnSpanCount = 2, RowSpanCount = 2,
						ChildContent = BuildWidget("Clock", PDWidgetType.Clock)
					},
					new PDDashboardTile
					{
						RowIndex = 0, ColumnIndex = 2, ColumnSpanCount = 2,
						ChildContent = BuildWidget("Status", PDWidgetType.Html, "<div class='p-3'><h5 class='text-success'>All Systems Operational</h5><p class='mb-0 text-muted'>Last checked: just now</p></div>")
					},
					new PDDashboardTile
					{
						RowIndex = 1, ColumnIndex = 2, ColumnSpanCount = 1,
						ChildContent = BuildWidget("Users", PDWidgetType.Html, "<div class='p-3 text-center'><div style='font-size:2rem;font-weight:300'>1,247</div><small class='text-muted'>Active Users</small></div>")
					},
					new PDDashboardTile
					{
						RowIndex = 1, ColumnIndex = 3, ColumnSpanCount = 1,
						ChildContent = BuildWidget("Alerts", PDWidgetType.Html, "<div class='p-3 text-center'><div style='font-size:2rem;font-weight:300' class='text-warning'>3</div><small class='text-muted'>Open Alerts</small></div>")
					},
					new PDDashboardTile
					{
						RowIndex = 2, ColumnIndex = 0, ColumnSpanCount = 4,
						ChildContent = BuildWidget("Activity", PDWidgetType.Html, "<div class='p-3'><h6>Recent Activity</h6><p class='mb-1 small'>User deployed v2.1.0 — 5 mins ago</p><p class='mb-1 small'>Alert resolved: CPU spike — 12 mins ago</p><p class='mb-0 small'>Backup completed — 1 hour ago</p></div>")
					}
				]
			},
			new PDDashboardTab
			{
				Name = "Metrics",
				Tiles =
				[
					new PDDashboardTile
					{
						RowIndex = 0, ColumnIndex = 0, ColumnSpanCount = 2, RowSpanCount = 2,
						ChildContent = BuildWidget("CPU Usage", PDWidgetType.Html, "<div class='p-3 text-center h-100 d-flex align-items-center justify-content-center'><div><div style='font-size:3rem;font-weight:200'>42%</div><small class='text-muted'>Average CPU</small></div></div>")
					},
					new PDDashboardTile
					{
						RowIndex = 0, ColumnIndex = 2, ColumnSpanCount = 2, RowSpanCount = 2,
						ChildContent = BuildWidget("Memory", PDWidgetType.Html, "<div class='p-3 text-center h-100 d-flex align-items-center justify-content-center'><div><div style='font-size:3rem;font-weight:200'>6.2 GB</div><small class='text-muted'>Used / 16 GB</small></div></div>")
					}
				]
			}
		];
	}

	private static RenderFragment BuildWidget(string title, PDWidgetType type, string? content = null) => builder =>
	{
		builder.OpenComponent<PDWidget>(0);
		builder.AddAttribute(1, nameof(PDWidget.Title), title);
		builder.AddAttribute(2, nameof(PDWidget.WidgetType), type);
		if (content is not null)
		{
			builder.AddAttribute(3, nameof(PDWidget.Content), content);
		}
		builder.CloseComponent();
	};

	private void OnTileMoved((PDDashboardTile Tile, int NewRow, int NewColumn) args)
	{
		EventManager?.Add(new Event("OnTileMove",
			new EventArgument("NewRow", args.NewRow),
			new EventArgument("NewColumn", args.NewColumn)));
	}

	private void OnTileResized((PDDashboardTile Tile, int NewRowSpan, int NewColumnSpan) args)
	{
		EventManager?.Add(new Event("OnTileResize",
			new EventArgument("NewRowSpan", args.NewRowSpan),
			new EventArgument("NewColumnSpan", args.NewColumnSpan)));
	}

	private void OnTabAdded(PDDashboardTab tab)
	{
		EventManager?.Add(new Event("OnTabAdd", new EventArgument("Name", tab.Name)));
	}

	private void OnActiveTabChanged(int index)
	{
		EventManager?.Add(new Event("ActiveTabChanged", new EventArgument("Index", index)));
	}
}
