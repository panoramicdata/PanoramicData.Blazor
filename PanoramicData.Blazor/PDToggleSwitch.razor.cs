namespace PanoramicData.Blazor;

public partial class PDToggleSwitch
{
	/// <summary>
	/// Sets the colour of the border.
	/// </summary>
	[Parameter] public string BorderColour { get; set; } = "#1b6ec2";

	/// <summary>
	/// Sets the width of the border.
	/// </summary>
	[Parameter] public int BorderWidth { get; set; } = 2;

	/// <summary>
	/// Sets the height of the component.
	/// </summary>
	[Parameter] public int Height { get; set; } = 20;

	/// <summary>
	/// Sets the background colour of the inner non-toggle area when the switch is off.
	/// </summary>
	[Parameter] public string OffBackgroundColour { get; set; } = "silver";

	/// <summary>
	/// Sets the foreground colour of the inner non-toggle area when the switch is off.
	/// </summary>
	/// <remarks>Leave blank to use toggle colour.</remarks>
	[Parameter] public string OffForegroundColour { get; set; } = string.Empty;

	/// <summary>
	/// Sets the text displayed when the switch is off.
	/// </summary>
	[Parameter] public string OffText { get; set; } = "OFF";

	/// <summary>
	/// Sets the background colour of the inner non-toggle area when the switch is on.
	/// </summary>
	/// <remarks>Leave blank to use border colour.</remarks>
	[Parameter] public string OnBackgroundColour { get; set; } = string.Empty;

	/// <summary>
	/// Sets the foreground colour of the inner non-toggle area when the switch is on.
	/// </summary>
	/// <remarks>Leave blank to use toggle colour.</remarks>
	[Parameter] public string OnForegroundColour { get; set; } = string.Empty;

	/// <summary>
	/// Sets the text displayed when the switch is on.
	/// </summary>
	[Parameter] public string OnText { get; set; } = "ON";

	/// <summary>
	/// Gets or sets whether the toggle switch has rounded ends.
	/// </summary>
	[Parameter] public bool Rounded { get; set; }

	/// <summary>
	/// Sets the colour of the toggle switch.
	/// </summary>
	[Parameter] public string ToggleColour { get; set; } = "white";

	/// <summary>
	/// Sets the value.
	/// </summary>
	[Parameter] public bool Value { get; set; }

	/// <summary>
	/// Event callback raised whenever the value changes.
	/// </summary>
	[Parameter] public EventCallback<bool> ValueChanged { get; set; }

	/// <summary>
	/// Sets the width of the component.
	/// </summary>
	[Parameter] public int Width { get; set; } = 50;

	#region Helper Properties

	private string InnerColour => Value
		? (string.IsNullOrWhiteSpace(OnBackgroundColour) ? BorderColour : OnBackgroundColour)
		: OffBackgroundColour;
	private double InnerHeight => Height - 2 - BorderWidth * 2;

	private string TextColour => Value
		? (string.IsNullOrWhiteSpace(OnForegroundColour) ? ToggleColour : OnForegroundColour)
		: (string.IsNullOrWhiteSpace(OffForegroundColour) ? ToggleColour : OffForegroundColour);

	#endregion

	public IDictionary<string, object> GetBackgroundAttributes()
	{
		return new Dictionary<string, object>
		{
			{ "height", Height - BorderWidth},
			{ "style", $"fill: {InnerColour}; stroke: {BorderColour}; stroke-width: {BorderWidth}" },
			{ "width", Width - BorderWidth },
			{ "x", BorderWidth / 2 },
			{ "y", BorderWidth / 2 },
			{ "rx", Rounded ? Height / 2 : 0 },
			{ "ry", Rounded ? Height / 2 : 0 }
		};
	}

	public IDictionary<string, object> GetTextAttributes()
	{
		return new Dictionary<string, object>
		{
			{ "style", $"font-size: {InnerHeight / 1.5}px; stroke: {TextColour}; fill: {TextColour}" },
			{ "text-anchor",  Value ? "start" : "end" },
			{ "x", Value ? BorderWidth * 3 : Width - BorderWidth * 3 },
			{ "y", InnerHeight / 1.6 + (InnerHeight / 3) }
		};
	}

	public IDictionary<string, object> GetToggleAttributes()
	{
		return new Dictionary<string, object>
		{
			{ "height", InnerHeight},
			{ "style", $"fill: {ToggleColour}; stroke: {ToggleColour}" },
			{ "width", Height - BorderWidth - 2},
			{ "x", Value ? Width - Height + BorderWidth - 1 : BorderWidth + 1 },
			{ "y", BorderWidth + 1 },
			{ "rx", Rounded ? Height / 2 : 0 },
			{ "ry", Rounded ? Height / 2 : 0 }
		};
	}

	private async Task OnClickAsync()
	{
		Value = !Value;
		await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
	}
}