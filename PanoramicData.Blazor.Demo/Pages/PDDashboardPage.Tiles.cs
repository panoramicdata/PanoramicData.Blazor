using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// The demo dashboard tabs, their tiles and widgets, and the widget events.
/// </summary>
public partial class PDDashboardPage
{
	private PDDashboardTile? _clockTile;

	private PDDashboardTab CreateOverviewTab()
	{
		_clockTile = new PDDashboardTile
		{
			RowIndex = 0, ColumnIndex = 0, ColumnSpanCount = 2, RowSpanCount = 2,
			ChildContent = BuildWidget("Clock", PDWidgetType.Clock, icon: "fas fa-clock")
		};

		return new PDDashboardTab
		{
			Name = "Overview",
			Tiles =
			[
				_clockTile,
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 2, ColumnSpanCount = 2,
					ChildContent = BuildWidget("Status", PDWidgetType.Html, "<div class='p-3'><h5 class='text-success'>All Systems Operational</h5><p class='mb-0 text-muted'>Last checked: just now</p></div>", icon: "fas fa-check-circle")
				},
				new PDDashboardTile
				{
					RowIndex = 1, ColumnIndex = 2, ColumnSpanCount = 1,
					ChildContent = BuildWidget("Users", PDWidgetType.Html, "<div class='p-3 text-center'><div style='font-size:2rem;font-weight:300'>1,247</div><small class='text-muted'>Active Users</small></div>", icon: "fas fa-users")
				},
				new PDDashboardTile
				{
					RowIndex = 1, ColumnIndex = 3, ColumnSpanCount = 1,
					ChildContent = BuildWidget("Alerts", PDWidgetType.Html, "<div class='p-3 text-center'><div style='font-size:2rem;font-weight:300' class='text-warning'>3</div><small class='text-muted'>Open Alerts</small></div>", icon: "fas fa-exclamation-triangle")
				},
				new PDDashboardTile
				{
					RowIndex = 2, ColumnIndex = 0, ColumnSpanCount = 4,
					ChildContent = BuildWidget("Activity", PDWidgetType.Html, "<div class='p-3'><h6>Recent Activity</h6><p class='mb-1 small'>User deployed v2.1.0 — 5 mins ago</p><p class='mb-1 small'>Alert resolved: CPU spike — 12 mins ago</p><p class='mb-0 small'>Backup completed — 1 hour ago</p></div>", icon: "fas fa-stream")
				}
			]
		};
	}

	private PDDashboardTab CreateMetricsTab()
		=> new()
		{
			Name = "Metrics",
			Tiles =
			[
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 0, ColumnSpanCount = 2, RowSpanCount = 2,
					ChildContent = BuildWidget("CPU Usage", PDWidgetType.Html, "<div class='p-3 text-center h-100 d-flex align-items-center justify-content-center'><div><div style='font-size:3rem;font-weight:200'>42%</div><small class='text-muted'>Average CPU</small></div></div>", icon: "fas fa-microchip")
				},
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 2, ColumnSpanCount = 2, RowSpanCount = 2,
					ChildContent = BuildWidget("Memory", PDWidgetType.Html, "<div class='p-3 text-center h-100 d-flex align-items-center justify-content-center'><div><div style='font-size:3rem;font-weight:200'>6.2 GB</div><small class='text-muted'>Used / 16 GB</small></div></div>", icon: "fas fa-memory")
				}
			]
		};

	private RenderFragment BuildWidget(string title, PDWidgetType type, string? content = null, string? icon = null) => builder =>
	{
		builder.OpenComponent<PDWidget>(0);
		builder.AddAttribute(1, nameof(PDWidget.Title), title);
		builder.AddAttribute(2, nameof(PDWidget.WidgetType), type);
		if (content is not null)
		{
			builder.AddAttribute(3, nameof(PDWidget.Content), content);
		}

		if (icon is not null)
		{
			builder.AddAttribute(4, nameof(PDWidget.Icon), icon);
		}

		var widgetTitle = title;
		builder.AddAttribute(5, nameof(PDWidget.ContentChanged),
			EventCallback.Factory.Create<string?>(this, c => OnWidgetContentChanged(widgetTitle, c)));
		builder.AddAttribute(6, nameof(PDWidget.WidgetTypeChanged),
			EventCallback.Factory.Create<PDWidgetType>(this, wt => OnWidgetTypeChanged(widgetTitle, wt)));
		builder.AddAttribute(7, nameof(PDWidget.TitleChanged),
			EventCallback.Factory.Create<string>(this, newTitle => OnWidgetTitleChanged(widgetTitle, newTitle)));
		builder.CloseComponent();
	};

	private void OnWidgetContentChanged(string widgetTitle, string? content)
		=> LogEvent("WidgetContentChanged", new EventArgument("Widget", widgetTitle), new EventArgument("ContentLength", content?.Length ?? 0));

	private void OnWidgetTypeChanged(string widgetTitle, PDWidgetType widgetType)
		=> LogEvent("WidgetTypeChanged", new EventArgument("Widget", widgetTitle), new EventArgument("NewType", widgetType));

	private void OnWidgetTitleChanged(string oldTitle, string newTitle)
		=> LogEvent("WidgetTitleChanged", new EventArgument("OldTitle", oldTitle), new EventArgument("NewTitle", newTitle));
}
