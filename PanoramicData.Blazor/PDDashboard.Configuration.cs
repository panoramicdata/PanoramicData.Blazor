namespace PanoramicData.Blazor;

/// <summary>
/// The settings of <see cref="PDDashboard"/>: the dashboard configuration dialog.
/// </summary>
public partial class PDDashboard
{
	// Dashboard configuration
	private bool _isConfiguringDashboard;
	private string _configName = string.Empty;
	private string _configTabName = string.Empty;
	private int _configColumnCount;
	private int _configRowHeight;
	private Dictionary<string, string> _configProperties = [];
	private string _newPropertyKey = string.Empty;
	private string _newPropertyValue = string.Empty;

	private void OpenDashboardConfig()
	{
		if (ActiveTab is not { } activeTab)
		{
			return;
		}

		_configName = Name ?? string.Empty;
		_configTabName = activeTab.Name;
		_configColumnCount = activeTab.ColumnCount ?? ColumnCount;
		_configRowHeight = activeTab.TileRowHeightPx ?? TileRowHeightPx;
		_configProperties = Properties is not null
			? new Dictionary<string, string>(Properties)
			: [];
		_newPropertyKey = string.Empty;
		_newPropertyValue = string.Empty;
		_isConfiguringDashboard = true;
	}

	private void CancelDashboardConfig()
	{
		_isConfiguringDashboard = false;
	}

	private async Task ApplyDashboardConfigAsync()
	{
		if (ActiveTab is { } activeTab)
		{
			activeTab.Name = _configTabName;
			activeTab.ColumnCount = _configColumnCount;
			activeTab.TileRowHeightPx = _configRowHeight;
		}

		Name = string.IsNullOrWhiteSpace(_configName) ? null : _configName.Trim();
		Properties = _configProperties.Count > 0 ? new Dictionary<string, string>(_configProperties) : null;

		_isConfiguringDashboard = false;

		await OnSettingsChanged.InvokeAsync().ConfigureAwait(true);

		StateHasChanged();
	}

	private void AddConfigProperty()
	{
		if (!string.IsNullOrWhiteSpace(_newPropertyKey))
		{
			_configProperties[_newPropertyKey.Trim()] = _newPropertyValue;
			_newPropertyKey = string.Empty;
			_newPropertyValue = string.Empty;
		}
	}

	private void RemoveConfigProperty(string key)
	{
		_configProperties.Remove(key);
	}

	private void UpdateConfigProperty(string key, string value)
	{
		_configProperties[key] = value;
	}
}
