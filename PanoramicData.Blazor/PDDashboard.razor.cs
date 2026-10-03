using Microsoft.AspNetCore.Components.Routing;
using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

/// <summary>
/// A tabbed dashboard container with CSS grid layout, kiosk/display mode,
/// tab rotation, and drag-and-drop tile editing.
/// </summary>
public partial class PDDashboard : PDComponentBase, IAsyncDisposable
{
	private static int _idSequence;
	private Timer? _rotationTimer;
	private bool _isUserInteracting;
	private bool _previousIsEditable;
	private bool _isInternallyEditable;
	private bool _previousIsRotationEnabled;
	private int _previousRotationIntervalSeconds;
	private bool _rotationTimerInitialized;
	private bool _isRotationPaused;

	// Maximize state
	private PDDashboardTile? _maximizedTile;

	[Inject] private NavigationManager NavigationManager { get; set; } = null!;

	/// <summary>
	/// Gets or sets the dashboard tabs.
	/// </summary>
	[Parameter]
	public List<PDDashboardTab> Tabs { get; set; } = [];

	/// <summary>
	/// Gets or sets the number of grid columns. Default 12.
	/// </summary>
	[Parameter]
	public int ColumnCount { get; set; } = 12;

	/// <summary>
	/// Gets or sets the height of each grid row in pixels.
	/// </summary>
	[Parameter]
	public int TileRowHeightPx { get; set; } = 120;

	/// <summary>
	/// Gets or sets dashboard-level CSS classes.
	/// </summary>
	[Parameter]
	public string? Css { get; set; }

	/// <summary>
	/// Gets or sets CSS classes applied to all widget headers. Individual widgets can override via HeaderCss.
	/// </summary>
	[Parameter]
	public string? WidgetHeaderCss { get; set; }

	/// <summary>
	/// Gets or sets CSS classes applied to all widget borders/cards. Individual widgets can override via BorderCss.
	/// </summary>
	[Parameter]
	public string? WidgetBorderCss { get; set; }

	/// <summary>
	/// Gets or sets CSS classes applied to all widget content areas. Individual widgets can override via ContentCss.
	/// </summary>
	[Parameter]
	public string? WidgetContentCss { get; set; }

	/// <summary>
	/// Gets or sets whether to show the tab bar.
	/// </summary>
	[Parameter]
	public bool ShowTabs { get; set; } = true;

	/// <summary>
	/// Gets or sets the index of the initially selected tab.
	/// </summary>
	[Parameter]
	public int StartTab { get; set; }

	/// <summary>
	/// Gets or sets whether automatic tab rotation is enabled.
	/// </summary>
	[Parameter]
	public bool IsRotationEnabled { get; set; }

	/// <summary>
	/// Gets or sets the tab auto-rotation interval in seconds. 0 = never rotate. Requires <see cref="IsRotationEnabled"/> to be true.
	/// </summary>
	[Parameter]
	public int RotationIntervalSeconds { get; set; } = 5;

	/// <summary>
	/// Gets or sets kiosk/display mode. When true, hides all editing chrome.
	/// </summary>
	[Parameter]
	public bool DisplayMode { get; set; }

	/// <summary>
	/// Gets or sets whether editing controls are enabled.
	/// </summary>
	[Parameter]
	public bool IsEditable { get; set; }

	/// <summary>
	/// Gets or sets the percentage of dashboard area used when a tile is maximized. Default 80.
	/// </summary>
	[Parameter]
	public int MaximizePercent { get; set; } = 80;

	/// <summary>
	/// Gets or sets whether the maximize button is shown in view mode.
	/// Individual tiles can override this via their ShowMaximize property.
	/// </summary>
	[Parameter]
	public bool ShowMaximize { get; set; }

	/// <summary>
	/// Gets or sets the dashboard display name.
	/// </summary>
	[Parameter]
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets whether to show the dashboard name in a header row above the tab bar.
	/// </summary>
	[Parameter]
	public bool ShowName { get; set; }

	/// <summary>
	/// Gets or sets what to display in the header row when in display mode.
	/// </summary>
	[Parameter]
	public DisplayModeHeaderContent DisplayModeHeader { get; set; } = DisplayModeHeaderContent.None;

	/// <summary>
	/// Gets or sets whether users in regular view (not display mode or edit mode) can override property values for their session.
	/// </summary>
	[Parameter]
	public bool AllowViewModePropertyEdit { get; set; }

	/// <summary>
	/// Gets or sets whether to show the built-in edit mode toggle button.
	/// Only shown when <see cref="IsEditable"/> is not forced on externally. Default true.
	/// </summary>
	[Parameter]
	public bool ShowEditButton { get; set; } = true;

	/// <summary>
	/// Gets or sets dashboard-level properties as string key/value pairs.
	/// These are cascaded to all widgets and can be overridden at the widget level.
	/// </summary>
	[Parameter]
	public Dictionary<string, string>? Properties { get; set; }

	/// <summary>
	/// Fired when a tile is moved via drag-and-drop.
	/// </summary>
	[Parameter]
	public EventCallback<(PDDashboardTile Tile, int NewRow, int NewColumn)> OnTileMove { get; set; }

	/// <summary>
	/// Fired when a tile is resized via the resize handle.
	/// </summary>
	[Parameter]
	public EventCallback<(PDDashboardTile Tile, int NewRowSpan, int NewColumnSpan)> OnTileResize { get; set; }

	/// <summary>
	/// Fired when the user requests to add a new tile. If no delegate is provided, a blank <see cref="PDWidget"/> tile is added automatically.
	/// </summary>
	[Parameter]
	public EventCallback OnTileAdd { get; set; }

	/// <summary>
	/// Fired when a tile is deleted. The tile has already been removed from the active tab.
	/// </summary>
	[Parameter]
	public EventCallback<PDDashboardTile> OnTileDelete { get; set; }

	/// <summary>
	/// Gets or sets whether tile deletion requires a confirmation dialog. Default true.
	/// </summary>
	[Parameter]
	public bool ConfirmTileDelete { get; set; } = true;

	/// <summary>
	/// Fired when a tab is added.
	/// </summary>
	[Parameter]
	public EventCallback<PDDashboardTab> OnTabAdd { get; set; }

	/// <summary>
	/// Fired when a tab is removed.
	/// </summary>
	[Parameter]
	public EventCallback<PDDashboardTab> OnTabRemove { get; set; }

	/// <summary>
	/// Fired when settings change.
	/// </summary>
	[Parameter]
	public EventCallback OnSettingsChanged { get; set; }

	/// <summary>
	/// Fired when the active tab changes.
	/// </summary>
	[Parameter]
	public EventCallback<int> ActiveTabChanged { get; set; }

	/// <summary>
	/// Fired when the IsEditable property changes value.
	/// </summary>
	[Parameter]
	public EventCallback<bool> OnEditModeChanged { get; set; }

	/// <summary>
	/// Gets the index of the currently active tab.
	/// </summary>
	public int ActiveTabIndex { get; private set; }

	/// <summary>
	/// Gets whether the dashboard is currently in edit mode, either via the <see cref="IsEditable"/> parameter or the built-in toggle.
	/// </summary>
	public bool EffectiveIsEditable => IsEditable || _isInternallyEditable;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		if (HasDefaultId)
		{
			Id = $"pd-dashboard-{Interlocked.Increment(ref _idSequence)}";
		}

		_previousIsEditable = IsEditable;

		if (Tabs.Count == 0)
		{
			Tabs.Add(new PDDashboardTab { Name = "Dashboard" });
		}

		// Read tab from URL deep link
		var uri = new Uri(NavigationManager.Uri);
		if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("tab", out var tabValue) &&
			int.TryParse(tabValue, CultureInfo.InvariantCulture, out var tabIndex) &&
			tabIndex >= 0 && tabIndex < Tabs.Count)
		{
			ActiveTabIndex = tabIndex;
		}
		else
		{
			ActiveTabIndex = StartTab;
		}
	}

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		if (_previousIsEditable != IsEditable)
		{
			_previousIsEditable = IsEditable;
			if (OnEditModeChanged.HasDelegate)
			{
				await OnEditModeChanged.InvokeAsync(EffectiveIsEditable).ConfigureAwait(true);
			}
		}

		if (_rotationTimerInitialized &&
			(_previousIsRotationEnabled != IsRotationEnabled || _previousRotationIntervalSeconds != RotationIntervalSeconds))
		{
			_previousIsRotationEnabled = IsRotationEnabled;
			_previousRotationIntervalSeconds = RotationIntervalSeconds;
			_rotationTimer?.Dispose();
			_rotationTimer = null;
			SetupRotationTimer();
		}
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{
			_previousIsRotationEnabled = IsRotationEnabled;
			_previousRotationIntervalSeconds = RotationIntervalSeconds;
			_rotationTimerInitialized = true;
			SetupRotationTimer();
		}
	}

	private async Task SelectTabAsync(int index)
	{
		if (index < 0 || index >= Tabs.Count || index == ActiveTabIndex)
		{
			return;
		}

		ActiveTabIndex = index;
		_isUserInteracting = true;

		// Update URL with deep link
		var uri = NavigationManager.GetUriWithQueryParameter("tab", index.ToString(CultureInfo.InvariantCulture));
		NavigationManager.NavigateTo(uri, replace: true);

		if (ActiveTabChanged.HasDelegate)
		{
			await ActiveTabChanged.InvokeAsync(index).ConfigureAwait(true);
		}

		ResetRotationTimer();
		StateHasChanged();
	}

	private void SetupRotationTimer()
	{
		var interval = GetEffectiveRotationInterval();
		if (interval > 0)
		{
			_rotationTimer = new Timer(_ =>
			{
				if (!_isUserInteracting && !_isRotationPaused)
				{
					InvokeAsync(() =>
					{
						ActiveTabIndex = (ActiveTabIndex + 1) % Tabs.Count;
						StateHasChanged();
					});
				}
			}, null, TimeSpan.FromSeconds(interval), TimeSpan.FromSeconds(interval));
		}
	}

	private void ResetRotationTimer()
	{
		_rotationTimer?.Dispose();
		_rotationTimer = null;
		_isUserInteracting = false;
		SetupRotationTimer();
	}

	/// <summary>
	/// Gets the rotation interval, in seconds, for the active tab: its own override if it has one, otherwise
	/// <see cref="RotationIntervalSeconds"/>. Zero, meaning never rotate, when rotation is off or the interval is
	/// not positive.
	/// </summary>
	internal int GetEffectiveRotationInterval()
	{
		if (!IsRotationEnabled)
		{
			return 0;
		}

		var interval = ActiveTab?.RotationIntervalSecondsOverride ?? RotationIntervalSeconds;
		return interval > 0 ? interval : 0;
	}

	private async Task ToggleEditModeAsync()
	{
		_isInternallyEditable = !_isInternallyEditable;

		if (_isInternallyEditable)
		{
			_rotationTimer?.Dispose();
			_rotationTimer = null;
		}
		else
		{
			SetupRotationTimer();
		}

		if (OnEditModeChanged.HasDelegate)
		{
			await OnEditModeChanged.InvokeAsync(EffectiveIsEditable).ConfigureAwait(true);
		}

		StateHasChanged();
	}

	private async Task ToggleRotationPauseAsync()
	{
		_isRotationPaused = !_isRotationPaused;
		StateHasChanged();
		await Task.CompletedTask.ConfigureAwait(true);
	}

	private async Task NavigatePreviousTabAsync()
	{
		var prev = (ActiveTabIndex - 1 + Tabs.Count) % Tabs.Count;
		await SelectTabAsync(prev).ConfigureAwait(true);
	}

	private async Task NavigateNextTabAsync()
	{
		var next = (ActiveTabIndex + 1) % Tabs.Count;
		await SelectTabAsync(next).ConfigureAwait(true);
	}

	// Maximize/Restore
	private void MaximizeTile(PDDashboardTile tile)
	{
		_maximizedTile = tile;
	}

	private void RestoreTile()
	{
		_maximizedTile = null;
	}

	private async Task AddTabAsync()
	{
		var newTab = new PDDashboardTab { Name = $"Tab {Tabs.Count + 1}" };
		Tabs.Add(newTab);

		if (OnTabAdd.HasDelegate)
		{
			await OnTabAdd.InvokeAsync(newTab).ConfigureAwait(true);
		}

		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync().ConfigureAwait(true);
		}

		await SelectTabAsync(Tabs.Count - 1).ConfigureAwait(true);
	}

	/// <summary>
	/// Removes a tab from the dashboard.
	/// </summary>
	public async Task RemoveTabAsync(int index)
	{
		if (index < 0 || index >= Tabs.Count)
		{
			return;
		}

		var tab = Tabs[index];
		Tabs.RemoveAt(index);

		if (OnTabRemove.HasDelegate)
		{
			await OnTabRemove.InvokeAsync(tab).ConfigureAwait(true);
		}

		if (OnSettingsChanged.HasDelegate)
		{
			await OnSettingsChanged.InvokeAsync().ConfigureAwait(true);
		}

		if (ActiveTabIndex >= Tabs.Count)
		{
			ActiveTabIndex = Math.Max(0, Tabs.Count - 1);
		}

		StateHasChanged();
	}

	/// <summary>
	/// Programmatically selects a tab by index.
	/// </summary>
	public async Task GoToTabAsync(int index)
	{
		await SelectTabAsync(index).ConfigureAwait(true);
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		_rotationTimer?.Dispose();
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
