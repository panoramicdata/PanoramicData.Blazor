namespace PanoramicData.Blazor;

/// <summary>
/// The session-level property overrides a viewer of <see cref="PDDashboard"/> can make in view mode.
/// </summary>
public partial class PDDashboard
{
	// View-mode property overrides (session-level, not persisted)
	private readonly Dictionary<string, string> _viewModePropertyOverrides = [];
	private bool _isEditingViewModeProperties;

	/// <summary>
	/// Gets the effective properties dictionary, merging <see cref="Properties"/> with any session-level view mode overrides.
	/// </summary>
	private Dictionary<string, string>? EffectiveProperties
	{
		get
		{
			if (_viewModePropertyOverrides.Count == 0)
			{
				return Properties;
			}

			var merged = Properties is not null
				? new Dictionary<string, string>(Properties)
				: [];
			foreach (var (k, v) in _viewModePropertyOverrides)
			{
				merged[k] = v;
			}

			return merged;
		}
	}

	private void OpenViewModePropertyEdit()
	{
		_isEditingViewModeProperties = true;
	}

	private void CloseViewModePropertyEdit()
	{
		_isEditingViewModeProperties = false;
	}

	private void SetViewModePropertyOverride(string key, string value)
	{
		_viewModePropertyOverrides[key] = value;
	}

	private void ResetViewModeProperties()
	{
		_viewModePropertyOverrides.Clear();
	}
}
