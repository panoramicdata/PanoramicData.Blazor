namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a toolbar with controls for navigating and configuring a <see cref="PDTimeline"/> component.
/// </summary>
public partial class PDTimelineToolbar : IEnablable
{
	/// <summary>
	/// Gets or sets whether the toolbar is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the date/time range of the timeline.
	/// </summary>
	[Parameter]
	public bool ShowRange { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the scale controls.
	/// </summary>
	[Parameter]
	public bool ShowScale { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the current selection details.
	/// </summary>
	[Parameter]
	public bool ShowSelection { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the zoom in/out buttons.
	/// </summary>
	[Parameter]
	public bool ShowZoomButtons { get; set; } = true;

	/// <summary>
	/// A reference to the PDTimeline component.
	/// </summary>
	[Parameter]
	public PDTimeline? Timeline { get; set; }

	private string MinDateTimeDisplay => (Timeline?.MinDateTime ?? DateTime.Now).ToString($"{Timeline?.Options.General.DateFormat ?? "d"} HH:mm:ss", CultureInfo.InvariantCulture);

	private string MaxDateTimeDisplay => (Timeline?.MaxDateTime ?? DateTime.Now).ToString($"{Timeline?.Options.General.DateFormat ?? "d"} HH:mm:ss", CultureInfo.InvariantCulture);
	private string SelectionEndTimeDisplay => (Timeline?.GetSelection()?.EndTime ?? DateTime.MinValue).ToString($"{Timeline?.Options.General.DateFormat ?? "d"} HH:mm:ss", CultureInfo.InvariantCulture);

	private string SelectionStartTimeDisplay => (Timeline?.GetSelection()?.StartTime ?? DateTime.MinValue).ToString($"{Timeline?.Options.General.DateFormat ?? "d"} HH:mm:ss", CultureInfo.InvariantCulture);

	/// <summary>
	/// Disables the toolbar.
	/// </summary>
	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	/// <summary>
	/// Enables the toolbar.
	/// </summary>
	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	/// <summary>
	/// Sets whether the toolbar is enabled.
	/// </summary>
	/// <param name="isEnabled">True to enable; false to disable.</param>
	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}
}
