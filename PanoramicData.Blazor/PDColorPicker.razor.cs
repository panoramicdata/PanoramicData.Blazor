using Microsoft.JSInterop;
using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor;

/// <summary>
/// Input mode for the color picker.
/// </summary>
public enum InputMode
{
	/// <summary>Red, Green, Blue colour model.</summary>
	RGB,
	/// <summary>Hue, Saturation, Value colour model.</summary>
	HSV,
	/// <summary>Six-digit hexadecimal colour notation.</summary>
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

	/// <summary>
	/// Gets or sets the saturation/value container element, set by the component markup.
	/// </summary>
	internal ElementReference SvContainerRef { get; set; }

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

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		_inputMode = GetInitialInputMode();
		_currentColor.SetFromHex(Value);
		_originalColor = _currentColor.Clone();
		_objRef = DotNetObjectReference.Create(this);
	}

	/// <summary>
	/// The input mode the picker opens with: the one for <see cref="ColorPickerOptions.DefaultMode"/> when that has
	/// been set (issue #177), otherwise RGB, which is what the picker has always opened with.
	/// </summary>
	private InputMode GetInitialInputMode()
	{
		if (!Options.IsDefaultModeSet)
		{
			return InputMode.RGB;
		}

		return Options.DefaultMode switch
		{
			ColorMode.HSV or ColorMode.HSL => InputMode.HSV,
			ColorMode.Hex => InputMode.Hex,
			_ => InputMode.RGB
		};
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (!_isOpen)
		{
			_currentColor.SetFromHex(Value);
			_originalColor = _currentColor.Clone();
		}
	}

	/// <inheritdoc />
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
			if (SvContainerRef.Id != null)
			{
				var bounds = await _module.InvokeAsync<ElementBounds?>("getElementBounds", SvContainerRef).ConfigureAwait(true);
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
			DisposeClickHandlerInBackground();
		}
	}

	private void ClosePicker()
	{
		_isOpen = false;
		DisposeClickHandlerInBackground();
		StateHasChanged();
	}

	// The JavaScript clean-up swallows its own errors, so it is safe to let it finish in the background.
	private void DisposeClickHandlerInBackground() => _ = DisposeClickHandlerAsync();

	private async Task DisposeClickHandlerAsync()
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

	internal sealed record ElementBounds(double Width, double Height, double Left, double Top);

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

	private async Task OnSvPointerDown(PointerEventArgs e)
	{
		_isDraggingSv = true;
		await UpdateSvFromPointerAsync(e).ConfigureAwait(true);
	}

	private async Task OnSvPointerMove(PointerEventArgs e)
	{
		if (_isDraggingSv)
		{
			await UpdateSvFromPointerAsync(e).ConfigureAwait(true);
		}
	}

	private void OnSvPointerUp()
	{
		_isDraggingSv = false;
	}

	private async Task UpdateSvFromPointerAsync(PointerEventArgs e)
	{
		// Use actual element dimensions if available, otherwise fall back to options
		var width = _svWidth > 0 ? _svWidth : Options.PopupWidth - 24; // Account for padding
		var height = _svHeight > 0 ? _svHeight : Options.SelectorHeight;

		var s = Math.Clamp(e.OffsetX / width, 0, 1);
		var v = Math.Clamp(1 - e.OffsetY / height, 0, 1);
		_currentColor.SetFromHsv(_currentColor.H, s, v);
		await NotifyColorChangeAsync().ConfigureAwait(true);
	}

	#endregion

	#region Hue Slider

	private async Task OnHuePointerDown(PointerEventArgs e)
	{
		_isDraggingHue = true;
		await UpdateHueFromPointerAsync(e).ConfigureAwait(true);
	}

	private async Task OnHuePointerMove(PointerEventArgs e)
	{
		if (_isDraggingHue)
		{
			await UpdateHueFromPointerAsync(e).ConfigureAwait(true);
		}
	}

	private void OnHuePointerUp()
	{
		_isDraggingHue = false;
	}

	private async Task UpdateHueFromPointerAsync(PointerEventArgs e)
	{
		var hue = Math.Clamp(e.OffsetX / (Options.PopupWidth - 20) * 360, 0, 360);
		_currentColor.SetFromHsv(hue, _currentColor.S, _currentColor.V);
		await NotifyColorChangeAsync().ConfigureAwait(true);
	}

	#endregion

	#region Alpha Slider

	private async Task OnAlphaPointerDown(PointerEventArgs e)
	{
		_isDraggingAlpha = true;
		await UpdateAlphaFromPointerAsync(e).ConfigureAwait(true);
	}

	private async Task OnAlphaPointerMove(PointerEventArgs e)
	{
		if (_isDraggingAlpha)
		{
			await UpdateAlphaFromPointerAsync(e).ConfigureAwait(true);
		}
	}

	private void OnAlphaPointerUp()
	{
		_isDraggingAlpha = false;
	}

	private async Task UpdateAlphaFromPointerAsync(PointerEventArgs e)
	{
		_currentColor.A = Math.Clamp(e.OffsetX / (Options.PopupWidth - 20), 0, 1);
		await NotifyColorChangeAsync().ConfigureAwait(true);
	}

	#endregion

	#region RGB Sliders

	private async Task OnRedSliderChange(ChangeEventArgs e)
	{
		if (byte.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetRgb(value, _currentColor.G, _currentColor.B);
			await NotifyColorChangeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnGreenSliderChange(ChangeEventArgs e)
	{
		if (byte.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetRgb(_currentColor.R, value, _currentColor.B);
			await NotifyColorChangeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnBlueSliderChange(ChangeEventArgs e)
	{
		if (byte.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetRgb(_currentColor.R, _currentColor.G, value);
			await NotifyColorChangeAsync().ConfigureAwait(true);
		}
	}

	#endregion

	#region HSV Sliders

	private async Task OnHueSliderChange(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetFromHsv(Math.Clamp(value, 0, 360), _currentColor.S, _currentColor.V);
			await NotifyColorChangeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnSaturationSliderChange(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetFromHsv(_currentColor.H, Math.Clamp(value / 100.0, 0, 1), _currentColor.V);
			await NotifyColorChangeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnValueSliderChange(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.SetFromHsv(_currentColor.H, _currentColor.S, Math.Clamp(value / 100.0, 0, 1));
			await NotifyColorChangeAsync().ConfigureAwait(true);
		}
	}

	#endregion

	#region Input Handlers

	private async Task OnHexInputChange(ChangeEventArgs e)
	{
		var hex = e.Value?.ToString() ?? "";
		_currentColor.SetFromHex(hex);
		await NotifyColorChangeAsync().ConfigureAwait(true);
	}

	private async Task OnRgbInputChange(ChangeEventArgs e, char component)
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
				default:
					_currentColor.SetRgb(_currentColor.R, _currentColor.G, value);
					break;
			}

			await NotifyColorChangeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnAlphaInputChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out var value))
		{
			_currentColor.A = Math.Clamp(value / 100.0, 0, 1);
			await NotifyColorChangeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnHsvInputChange(ChangeEventArgs e, char component)
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
				default:
					_currentColor.SetFromHsv(_currentColor.H, _currentColor.S, Math.Clamp(value / 100.0, 0, 1));
					break;
			}

			await NotifyColorChangeAsync().ConfigureAwait(true);
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
		await NotifyColorChangeAsync().ConfigureAwait(true);

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

	private async Task RevertColor()
	{
		_currentColor = _originalColor.Clone();
		await NotifyColorChangeAsync().ConfigureAwait(true);
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

	private async Task CancelSelection()
	{
		_currentColor = _originalColor.Clone();

		// Issue #99: live preview has already pushed intermediate values to the caller as the
		// user dragged, so cancelling has to send the original back. Restoring only the local
		// state left the component reverted and the caller holding the colour that was being
		// dragged towards - which, for a caller that renders on every change, meant cancelling
		// kept the abandoned colour.
		if (Options.LivePreview)
		{
			await ValueChanged.InvokeAsync(GetOutputColorValue()).ConfigureAwait(true);
		}

		ClosePicker();
	}

	private async Task NotifyColorChangeAsync()
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

	/// <inheritdoc />
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
