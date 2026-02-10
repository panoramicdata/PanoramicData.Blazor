using Microsoft.JSInterop;
using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor;

/// <summary>
/// Input mode for the color picker.
/// </summary>
public enum InputMode
{
	RGB,
	HSV,
	Hex
}

/// <summary>
/// A color picker component with support for multiple color modes,
/// color space selectors, palettes, and recent colors.
/// </summary>
public partial class PDColorPicker : IAsyncDisposable
{
	private static int _seq;
	private bool _isOpen;
	private bool _isDraggingSv;
	private bool _isDraggingHue;
	private bool _isDraggingAlpha;
	private InputMode _inputMode = InputMode.RGB;
	private ColorValue _currentColor = new();
	private ColorValue _originalColor = new();
	private DotNetObjectReference<PDColorPicker>? _objRef;
	private IJSObjectReference? _module;
	private ElementReference _svContainerRef;
	private ElementReference _hueStripRef;
	private double _svWidth;
	private double _svHeight;

	[Inject]
	private IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the unique identifier.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-colorpicker-{++_seq}";

	/// <summary>
	/// Gets or sets the current color value (hex format).
	/// </summary>
	[Parameter]
	public string Value { get; set; } = "#000000";

	/// <summary>
	/// Event callback raised when the color value changes.
	/// </summary>
	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	/// <summary>
	/// Event callback raised when a color is selected (after confirmation if buttons shown).
	/// </summary>
	[Parameter]
	public EventCallback<string> ColorSelected { get; set; }

	/// <summary>
	/// Gets or sets the button sizes.
	/// </summary>
	[Parameter]
	public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Gets or sets the text displayed on the button.
	/// </summary>
	[Parameter]
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets CSS classes for the button.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = "btn-secondary";

	/// <summary>
	/// Gets or sets CSS classes for the toolbar item.
	/// </summary>
	[Parameter]
	public string ItemCssClass { get; set; } = "";

	/// <summary>
	/// Gets or sets CSS classes for the text.
	/// </summary>
	[Parameter]
	public string TextCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tooltip for the toolbar item.
	/// </summary>
	[Parameter]
	public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	[Parameter]
	public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is positioned further to the right.
	/// </summary>
	[Parameter]
	public bool ShiftRight { get; set; }

	/// <summary>
	/// Gets or sets the color picker options.
	/// </summary>
	[Parameter]
	public ColorPickerOptions Options { get; set; } = new();

	/// <summary>
	/// Gets or sets the color palette to display.
	/// </summary>
	[Parameter]
	public List<PaletteColor>? Palette { get; set; }

	/// <summary>
	/// Gets or sets the recently chosen colors.
	/// </summary>
	[Parameter]
	public List<string>? RecentColors { get; set; }

	/// <summary>
	/// Event callback raised when recent colors should be updated.
	/// </summary>
	[Parameter]
	public EventCallback<List<string>> RecentColorsChanged { get; set; }

	private string ButtonSizeCssClass => Size switch
	{
		ButtonSizes.Small => "btn-sm",
		ButtonSizes.Large => "btn-lg",
		_ => string.Empty
	};

	protected override void OnInitialized()
	{
		_currentColor.SetFromHex(Value);
		_originalColor = _currentColor.Clone();
		_objRef = DotNetObjectReference.Create(this);
	}

	protected override void OnParametersSet()
	{
		if (!_isOpen)
		{
			_currentColor.SetFromHex(Value);
			_originalColor = _currentColor.Clone();
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
		_module = await JSRuntime.InvokeAsync<IJSObjectReference>(
				"import",
				"./_content/PanoramicData.Blazor/PDColorPicker.razor.js"
			).ConfigureAwait(true);
		}

		if (_isOpen && Options.CloseOnOutsideClick && _module != null)
		{
			await _module.InvokeVoidAsync("initialize", Id, _objRef).ConfigureAwait(true);

			// Get the actual dimensions of the SV container for accurate positioning
			if (_svContainerRef.Id != null)
			{
				var bounds = await _module.InvokeAsync<ElementBounds?>("getElementBounds", _svContainerRef).ConfigureAwait(true);
				if (bounds != null)
				{
					_svWidth = bounds.Width;
					_svHeight = bounds.Height;
				}
			}
		}
	}

	/// <summary>
	/// Called from JavaScript when user clicks outside the picker.
	/// </summary>
	[JSInvokable]
	public void OnOutsideClick()
	{
		if (_isOpen)
		{
			ClosePicker();
		}
	}

	private void TogglePicker()
	{
		_isOpen = !_isOpen;
		if (_isOpen)
		{
			_currentColor.SetFromHex(Value);
			_originalColor = _currentColor.Clone();
		}
		else
		{
			DisposeClickHandler();
		}
	}

	private void ClosePicker()
	{
		_isOpen = false;
		DisposeClickHandler();
		StateHasChanged();
	}

	private async void DisposeClickHandler()
	{
		try
		{
			if (_module != null)
			{
				await _module.InvokeVoidAsync("dispose", Id).ConfigureAwait(true);
			}
		}
		catch
		{
			// Ignore JS errors during cleanup
		}
	}

	private sealed record ElementBounds(double Width, double Height, double Left, double Top);

	private string GetSwatchBackground()
	{
		if (string.IsNullOrWhiteSpace(Value))
		{
			return "transparent";
		}

		var color = ColorValue.FromHex(Value);
		return color.A < 1.0 ? color.ToRgba() : color.ToHex();
	}

	private string GetHexInputValue()
	{
		return Options.AllowTransparency && _currentColor.A < 1.0
			? _currentColor.ToHexWithAlpha()
			: _currentColor.ToHex();
	}

	private bool IsSelectedPaletteColor(string color)
	{
		var paletteColor = ColorValue.FromHex(color);
		return _currentColor.R == paletteColor.R &&
			   _currentColor.G == paletteColor.G &&
			   _currentColor.B == paletteColor.B;
	}

	#region Saturation/Value Selector

	private void OnSvPointerDown(PointerEventArgs e)
	{
		_isDraggingSv = true;
		UpdateSvFromPointer(e);
	}

	private void OnSvPointerMove(PointerEventArgs e)
	{
		if (_isDraggingSv)
		{
			UpdateSvFromPointer(e);
		}
	}

	private void OnSvPointerUp(PointerEventArgs e)
	{
		_isDraggingSv = false;
	}

	private void UpdateSvFromPointer(PointerEventArgs e)
	{
		// Use actual element dimensions if available, otherwise fall back to options
		var width = _svWidth > 0 ? _svWidth : Options.PopupWidth - 24; // Account for padding
		var height = _svHeight > 0 ? _svHeight : Options.SelectorHeight;

		var s = Math.Clamp(e.OffsetX / width, 0, 1);
		var v = Math.Clamp(1 - e.OffsetY / height, 0, 1);
		_currentColor.SetFromHsv(_currentColor.H, s, v);
		NotifyColorChange();
	}

	#endregion

	#region Hue Slider

	private void OnHuePointerDown(PointerEventArgs e)
	{
		_isDraggingHue = true;
		UpdateHueFromPointer(e);
	}

	private void OnHuePointerMove(PointerEventArgs e)
	{
		if (_isDraggingHue)
		{
			UpdateHueFromPointer(e);
		}
	}

	private void OnHuePointerUp(PointerEventArgs e)
	{
		_isDraggingHue = false;
	}

	private void UpdateHueFromPointer(PointerEventArgs e)
	{
		var hue = Math.Clamp(e.OffsetX / (Options.PopupWidth - 20) * 360, 0, 360);
		_currentColor.SetFromHsv(hue, _currentColor.S, _currentColor.V);
		NotifyColorChange();
	}

	#endregion

	#region Alpha Slider

	private void OnAlphaPointerDown(PointerEventArgs e)
	{
		_isDraggingAlpha = true;
		UpdateAlphaFromPointer(e);
	}

	private void OnAlphaPointerMove(PointerEventArgs e)
	{
		if (_isDraggingAlpha)
		{
			UpdateAlphaFromPointer(e);
		}
	}

	private void OnAlphaPointerUp(PointerEventArgs e)
	{
		_isDraggingAlpha = false;
	}

	private void UpdateAlphaFromPointer(PointerEventArgs e)
	{
		_currentColor.A = Math.Clamp(e.OffsetX / (Options.PopupWidth - 20), 0, 1);
		NotifyColorChange();
	}

	#endregion

	#region RGB Sliders

	private void OnRedSliderChange(ChangeEventArgs e)
	{
		if (byte.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetRgb(value, _currentColor.G, _currentColor.B);
			NotifyColorChange();
		}
	}

	private void OnGreenSliderChange(ChangeEventArgs e)
	{
		if (byte.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetRgb(_currentColor.R, value, _currentColor.B);
			NotifyColorChange();
		}
	}

	private void OnBlueSliderChange(ChangeEventArgs e)
	{
		if (byte.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetRgb(_currentColor.R, _currentColor.G, value);
			NotifyColorChange();
		}
	}

	#endregion

	#region HSV Sliders

	private void OnHueSliderChange(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetFromHsv(Math.Clamp(value, 0, 360), _currentColor.S, _currentColor.V);
			NotifyColorChange();
		}
	}

	private void OnSaturationSliderChange(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetFromHsv(_currentColor.H, Math.Clamp(value / 100.0, 0, 1), _currentColor.V);
			NotifyColorChange();
		}
	}

	private void OnValueSliderChange(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetFromHsv(_currentColor.H, _currentColor.S, Math.Clamp(value / 100.0, 0, 1));
			NotifyColorChange();
		}
	}

	#endregion

	#region Input Handlers

	private void OnHexInputChange(ChangeEventArgs e)
	{
		var hex = e.Value?.ToString() ?? "";
		_currentColor.SetFromHex(hex);
		NotifyColorChange();
	}

	private void OnRgbInputChange(ChangeEventArgs e, char component)
	{
		if (byte.TryParse(e.Value?.ToString(), out var value))
		{
			switch (component)
			{
				case 'R':
					_currentColor.SetRgb(value, _currentColor.G, _currentColor.B);
					break;
				case 'G':
					_currentColor.SetRgb(_currentColor.R, value, _currentColor.B);
					break;
				case 'B':
					_currentColor.SetRgb(_currentColor.R, _currentColor.G, value);
					break;
			}

			NotifyColorChange();
		}
	}

	private void OnAlphaInputChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.A = Math.Clamp(value / 100.0, 0, 1);
			NotifyColorChange();
		}
	}

	private void OnHsvInputChange(ChangeEventArgs e, char component)
	{
		if (double.TryParse(e.Value?.ToString(), out var value))
		{
			switch (component)
			{
				case 'H':
					_currentColor.SetFromHsv(Math.Clamp(value, 0, 360), _currentColor.S, _currentColor.V);
					break;
				case 'S':
					_currentColor.SetFromHsv(_currentColor.H, Math.Clamp(value / 100.0, 0, 1), _currentColor.V);
					break;
				case 'V':
					_currentColor.SetFromHsv(_currentColor.H, _currentColor.S, Math.Clamp(value / 100.0, 0, 1));
					break;
			}

			NotifyColorChange();
		}
	}

	private void ToggleInputMode()
	{
		// Cycle through available modes: RGB -> HSV -> Hex -> RGB...
		var modes = new List<InputMode>();
		if (Options.EnabledModes.HasFlag(ColorMode.RGB))
		{
			modes.Add(InputMode.RGB);
		}

		if (Options.EnabledModes.HasFlag(ColorMode.HSV))
		{
			modes.Add(InputMode.HSV);
		}

		if (Options.EnabledModes.HasFlag(ColorMode.Hex))
		{
			modes.Add(InputMode.Hex);
		}

		if (modes.Count == 0)
		{
			return;
		}

		var currentIndex = modes.IndexOf(_inputMode);
		_inputMode = modes[(currentIndex + 1) % modes.Count];
	}

	#endregion

	#region Palette & Recent Colors

	private async Task SelectPaletteColor(string color)
	{
		_currentColor.SetFromHex(color);
		NotifyColorChange();

		if (Options.CloseOnSelect && !Options.ShowButtons)
		{
			await ConfirmSelection().ConfigureAwait(true);
		}
	}

	private async Task SelectNoColor()
	{
		_currentColor = new ColorValue { A = 0 };
		await ValueChanged.InvokeAsync("transparent").ConfigureAwait(true);
		await ColorSelected.InvokeAsync("transparent").ConfigureAwait(true);
		ClosePicker();
	}

	private void RevertColor()
	{
		_currentColor = _originalColor.Clone();
		NotifyColorChange();
	}

	#endregion

	#region Confirmation

	private async Task ConfirmSelection()
	{
		var colorValue = GetOutputColorValue();
		await UpdateRecentColors(colorValue).ConfigureAwait(true);
		await ValueChanged.InvokeAsync(colorValue).ConfigureAwait(true);
		await ColorSelected.InvokeAsync(colorValue).ConfigureAwait(true);
		ClosePicker();
	}

	private void CancelSelection()
	{
		_currentColor = _originalColor.Clone();
		ClosePicker();
	}

	private async void NotifyColorChange()
	{
		if (Options.LivePreview)
		{
			await ValueChanged.InvokeAsync(GetOutputColorValue()).ConfigureAwait(true);
		}

		StateHasChanged();
	}

	private string GetOutputColorValue()
	{
		return Options.AllowTransparency && _currentColor.A < 1.0
			? _currentColor.ToRgba()
			: _currentColor.ToHex();
	}

	private async Task UpdateRecentColors(string color)
	{
		if (RecentColors == null || !Options.ShowRecentColors)
		{
			return;
		}

		// Remove if already exists (will be re-added at front)
		RecentColors.Remove(color);

		// Add to front
		RecentColors.Insert(0, color);

		// Trim to max
		while (RecentColors.Count > Options.MaxRecentColors)
		{
			RecentColors.RemoveAt(RecentColors.Count - 1);
		}

		await RecentColorsChanged.InvokeAsync(RecentColors).ConfigureAwait(true);
	}

	#endregion

	public async ValueTask DisposeAsync()
	{
		if (_module != null)
		{
			try
			{
				await _module.InvokeVoidAsync("dispose", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}
			catch
			{
				// Ignore JS errors during cleanup
			}
		}

		_objRef?.Dispose();
		GC.SuppressFinalize(this);
	}
}
