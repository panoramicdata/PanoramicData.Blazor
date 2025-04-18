﻿namespace PanoramicData.Blazor;

public partial class PDToolbarTextbox : IEnablable
{
	/// <summary>
	/// Gets or sets the text box input type
	/// </summary>
	[Parameter] public PDInputType Type { get; set; } = PDInputType.Text;

	/// <summary>
	/// Gets or sets the text box sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier.
	/// </summary>
	[Parameter] public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets CSS classes for the text box.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets CSS classes for the toolbar item.
	/// </summary>
	[Parameter] public string ItemCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets whether Keypress events are raised.
	/// </summary>
	[Parameter] public bool KeypressEvent { get; set; }

	/// <summary>
	/// Gets or sets placeholder text for the text box.
	/// </summary>
	[Parameter] public string Placeholder { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tooltip for the toolbar item.
	/// </summary>
	[Parameter] public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	[Parameter] public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is enabled.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item.
	/// </summary>
	[Parameter] public bool ShiftRight { get; set; }

	/// <summary>
	/// Sets the width of the containing div element.
	/// </summary>
	[Parameter] public string Width { get; set; } = "Auto";

	/// <summary>
	/// Sets the initial text value.
	/// </summary>
	[Parameter] public string Value { get; set; } = string.Empty;

	/// <summary>
	/// Event raised whenever the text value changes.
	/// </summary>
	[Parameter] public EventCallback<string> ValueChanged { get; set; }

	/// <summary>
	/// Event raised whenever a key is pressed.
	/// </summary>
	[Parameter] public EventCallback<KeyboardEventArgs> Keypress { get; set; }

	/// <summary>
	/// Gets or sets whether the clear button is displayed.
	/// </summary>
	[Parameter] public bool ShowClearButton { get; set; } = true;

	/// <summary>
	/// Event raised when the user clicks on the clear button.
	/// </summary>
	[Parameter] public EventCallback Cleared { get; set; }

	/// <summary>
	/// Sets an optional label to be displayed before the text box.
	/// </summary>
	[Parameter] public string Label { get; set; } = string.Empty;

	/// <summary>
	/// Sets the de-bounce wait period in milliseconds.
	/// </summary>
	[Parameter] public int DebounceWait { get; set; }

	public string ItemStyle => $"width: {Width}";

	private async Task OnKeypress(KeyboardEventArgs args) => await Keypress.InvokeAsync(args).ConfigureAwait(true);

	private async Task OnCleared()
	{
		Value = string.Empty;
		await ValueChanged.InvokeAsync(string.Empty).ConfigureAwait(true);
		await Cleared.InvokeAsync(null).ConfigureAwait(true);
	}

	private async Task OnValueChanged(string value)
	{
		if (value != Value)
		{
			Value = value;
			await ValueChanged.InvokeAsync(value).ConfigureAwait(true);
		}
	}
	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}
}
