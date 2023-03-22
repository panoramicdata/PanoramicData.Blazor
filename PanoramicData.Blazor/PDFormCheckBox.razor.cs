namespace PanoramicData.Blazor;

public partial class PDFormCheckBox
{
	[Parameter] public string CssClass { get; set; } = string.Empty;

	[Parameter] public bool Disabled { get; set; }

	[Parameter] public string Label { get; set; } = string.Empty;

	[Parameter] public bool LabelBefore { get; set; }

	[Parameter] public bool Value { get; set; }

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
