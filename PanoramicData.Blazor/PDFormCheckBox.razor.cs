namespace PanoramicData.Blazor;

public partial class PDFormCheckBox
{
	/// <summary>
	/// Gets or sets the CSS class for the checkbox.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the checkbox is disabled.
	/// </summary>
	[Parameter] public bool Disabled { get; set; }

	/// <summary>
	/// Gets or sets the label for the checkbox.
	/// </summary>
	[Parameter] public string Label { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the label should be displayed before the checkbox.
	/// </summary>
	[Parameter] public bool LabelBefore { get; set; }

	/// <summary>
	/// Gets or sets the current value of the checkbox.
	/// </summary>
	[Parameter] public bool Value { get; set; }

	/// <summary>
	/// An event callback that is invoked when the checkbox value changes.
	/// </summary>
	[Parameter] public EventCallback<bool> ValueChanged { get; set; }

	private void OnClick()
	{
		if (!Disabled)
		{
			ToggleValue();
		}
	}

	private void OnKeyPress(KeyboardEventArgs args)
	{
		if (!Disabled && args.Code.In("Space", "Enter"))
		{
			ToggleValue();
		}
	}

	private void ToggleValue()
	{
		Value = !Value;
		ValueChanged.InvokeAsync(Value);
	}
}
