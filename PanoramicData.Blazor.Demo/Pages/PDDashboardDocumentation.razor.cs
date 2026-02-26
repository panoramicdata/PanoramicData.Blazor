using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDashboardDocumentation
{
	private List<PDDashboardTab> _basicTabs =
	[
		new PDDashboardTab
		{
			Name = "Tab 1",
			Tiles =
			[
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 0, ColumnSpanCount = 2,
					ChildContent = builder =>
					{
						builder.OpenComponent<PDWidget>(0);
						builder.AddAttribute(1, nameof(PDWidget.Title), "Widget A");
						builder.AddAttribute(2, nameof(PDWidget.WidgetType), PDWidgetType.Html);
						builder.AddAttribute(3, nameof(PDWidget.Content), "<div class='p-3'>Hello, Dashboard!</div>");
						builder.CloseComponent();
					}
				},
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 2, ColumnSpanCount = 2,
					ChildContent = builder =>
					{
						builder.OpenComponent<PDWidget>(0);
						builder.AddAttribute(1, nameof(PDWidget.Title), "Widget B");
						builder.AddAttribute(2, nameof(PDWidget.WidgetType), PDWidgetType.Clock);
						builder.AddAttribute(3, nameof(PDWidget.Icon), "fas fa-clock");
						builder.CloseComponent();
					}
				}
			]
		}
	];

	private List<PDDashboardTab> _editTabs =
	[
		new PDDashboardTab
		{
			Name = "Editable",
			Tiles =
			[
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 0, ColumnSpanCount = 2,
					ChildContent = builder =>
					{
						builder.OpenComponent<PDWidget>(0);
						builder.AddAttribute(1, nameof(PDWidget.Title), "Drag Me");
						builder.AddAttribute(2, nameof(PDWidget.WidgetType), PDWidgetType.Html);
						builder.AddAttribute(3, nameof(PDWidget.Content), "<div class='p-3'>Try dragging or resizing this tile.</div>");
						builder.CloseComponent();
					}
				},
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 2, ColumnSpanCount = 2,
					ChildContent = builder =>
					{
						builder.OpenComponent<PDWidget>(0);
						builder.AddAttribute(1, nameof(PDWidget.Title), "Another Tile");
						builder.AddAttribute(2, nameof(PDWidget.WidgetType), PDWidgetType.Html);
						builder.AddAttribute(3, nameof(PDWidget.Content), "<div class='p-3 text-center'><strong>42</strong><br/><small class='text-muted'>Metric</small></div>");
						builder.CloseComponent();
					}
				}
			]
		}
	];

	private List<PDDashboardTab> _maximizeTabs =
	[
		new PDDashboardTab
		{
			Name = "Maximize Demo",
			Tiles =
			[
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 0, ColumnSpanCount = 2,
					ChildContent = builder =>
					{
						builder.OpenComponent<PDWidget>(0);
						builder.AddAttribute(1, nameof(PDWidget.Title), "Maximizable");
						builder.AddAttribute(2, nameof(PDWidget.WidgetType), PDWidgetType.Html);
						builder.AddAttribute(3, nameof(PDWidget.Content), "<div class='p-3'>Hover to see the maximize button.</div>");
						builder.AddAttribute(4, nameof(PDWidget.Icon), "fas fa-expand");
						builder.CloseComponent();
					}
				},
				new PDDashboardTile
				{
					RowIndex = 0, ColumnIndex = 2, ColumnSpanCount = 2,
					ShowMaximize = false,
					ChildContent = builder =>
					{
						builder.OpenComponent<PDWidget>(0);
						builder.AddAttribute(1, nameof(PDWidget.Title), "No Maximize");
						builder.AddAttribute(2, nameof(PDWidget.WidgetType), PDWidgetType.Html);
						builder.AddAttribute(3, nameof(PDWidget.Content), "<div class='p-3'>This tile overrides ShowMaximize to false.</div>");
						builder.CloseComponent();
					}
				}
			]
		}
	];

	private const string _basicExample = """
		<PDDashboard Tabs="_tabs"
		             ColumnCount="4"
		             TileRowHeightPx="120" />

		@code {
		    private List<PDDashboardTab> _tabs =
		    [
		        new PDDashboardTab
		        {
		            Name = "Tab 1",
		            Tiles =
		            [
		                new PDDashboardTile
		                {
		                    RowIndex = 0,
		                    ColumnIndex = 0,
		                    ColumnSpanCount = 2,
		                    ChildContent = builder =>
		                    {
		                        builder.OpenComponent<PDWidget>(0);
		                        builder.AddAttribute(1, "Title", "Widget A");
		                        builder.AddAttribute(2, "WidgetType", PDWidgetType.Html);
		                        builder.AddAttribute(3, "Content", "<div>Hello!</div>");
		                        builder.CloseComponent();
		                    }
		                }
		            ]
		        }
		    ];
		}
		""";

	private const string _editableExample = """
		@* Edit mode is built-in — click the ✏ Edit button in the tab bar *@
		<PDDashboard Tabs="_tabs"
					 ColumnCount="4"
					 TileRowHeightPx="120"
					 OnTileMove="OnTileMoved"
					 OnTileResize="OnTileResized"
					 OnTileDelete="OnTileDeleted"
					 OnSettingsChanged="OnSettingsChanged" />

		@* To force edit mode on externally, or hide the built-in toggle: *@
		@* IsEditable="true" ShowEditButton="false" *@
		""";

	private const string _maximizeExample = """
		<PDDashboard Tabs="_tabs"
		             ColumnCount="4"
		             ShowMaximize="true" />

		@* Per-tile override: *@
		new PDDashboardTile
		{
		    ShowMaximize = false, // Override: no maximize
		    ChildContent = ...
		}
		""";
}
