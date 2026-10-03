using PanoramicData.Blazor.Options;

namespace PanoramicData.Blazor;

/// <summary>
/// Toggle switch component with optional labels and JS-based text measurement.
/// </summary>
public partial class PDToggleSwitch : IAsyncDisposable
{
	private static int _sequence;

	private double _textWidth;
	private IJSObjectReference? _module;
	private readonly string _textCache = string.Empty;

	/// <summary>
	/// Gets or sets JavaScript runtime used by this component.
	/// </summary>
	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the border width of the switch.
	/// </summary>
	[Parameter] public int? BorderWidth { get; set; }

	/// <summary>
	/// Gets or sets the height of the switch.
	/// </summary>
	[Parameter] public int? Height { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier for the component.
	/// </summary>
	[Parameter] public override string Id { get; set; } = $"pd-toggleswitch-{++_sequence}";

	/// <summary>
	/// Gets or sets the label text for the switch.
	/// </summary>
	[Parameter] public string Label { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the label should be displayed before the switch.
	/// </summary>
	[Parameter] public bool? LabelBefore { get; set; }

	/// <summary>
	/// Gets or sets the text to display when the switch is in the 'off' state.
	/// </summary>
	[Parameter] public string? OffText { get; set; }

	/// <summary>
	/// Gets or sets the text to display when the switch is in the 'on' state.
	/// </summary>
	[Parameter] public string? OnText { get; set; }

	/// <summary>
	/// Gets or sets additional CSS classes applied to the SVG text element.
	/// </summary>
	[Parameter] public string? TextCssClass { get; set; }

	/// <summary>
	/// Gets or sets the options for the toggle switch.
	/// </summary>
	[Parameter] public PDToggleSwitchOptions Options { get; set; } = new();

	/// <summary>
	/// Gets or sets whether the switch is rounded.
	/// </summary>
	[Parameter] public bool? Rounded { get; set; }

	/// <summary>
	/// Gets or sets the current value of the switch.
	/// </summary>
	[Parameter] public bool Value { get; set; }

	/// <summary>
	/// An event callback that is invoked when the switch value changes.
	/// </summary>
	[Parameter] public EventCallback<bool> ValueChanged { get; set; }

	/// <summary>
	/// Gets or sets the width of the switch.
	/// </summary>
	[Parameter] public int? Width { get; set; }

	#region Helper Properties

	private double CalculatedHeight => Height ?? Options.Height ?? (Size ?? Options.Size) switch
	{
		ButtonSizes.Small => 16,
		ButtonSizes.Large => 32,
		_ => 24
	};

	private double CalculatedWidth => Width ?? Options.Width ?? (Size ?? Options.Size) switch
	{
		ButtonSizes.Small => 32 + TextWidthBeyond(8),
		ButtonSizes.Large => 64 + TextWidthBeyond(24),
		_ => 48 + TextWidthBeyond(16),
	};

	/// <summary>
	/// The measured text width beyond the room the base width already leaves for text, never negative, so a
	/// longer text never makes the switch narrower (issue #188).
	/// </summary>
	/// <param name="room">The text width the base width already accommodates.</param>
	private double TextWidthBeyond(double room) => Math.Max(0, _textWidth - room);

	private double InnerHeight => CalculatedHeight - 2 - EffectiveBorderWidth * 2;

	private int EffectiveBorderWidth => BorderWidth ?? Options.BorderWidth;

	private double CornerRadius => (Rounded ?? Options.Rounded) ? CalculatedHeight / 2 : 0;

	private string StateCssClass => Value ? "on" : "off";

	private string SizeCssClass => (Size ?? Options.Size) switch
	{
		ButtonSizes.Small => "sm",
		ButtonSizes.Large => "lg",
		_ => "md"
	};

	private int TextYOffset => (Size ?? Options.Size) switch
	{
		ButtonSizes.Small => 1,
		ButtonSizes.Large => -1,
		_ => 0
	};

	#endregion

	/// <summary>
	/// Disposes JS resources associated with this component.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				await _module.DisposeAsync().ConfigureAwait(true);
				_module = null;
			}
		}
		catch
		{
			// BC-40 - the circuit may already be gone, in which case there is no JavaScript side left to tear down
		}
	}

	/// <summary>
	/// Builds SVG attributes for the switch background.
	/// </summary>
	/// <returns>Attribute dictionary.</returns>
	public IDictionary<string, object> GetBackgroundAttributes() => new Dictionary<string, object>
		{
			{ "class", $"switch {StateCssClass}"},
			{ "height", CalculatedHeight - EffectiveBorderWidth},
			{ "width", CalculatedWidth - EffectiveBorderWidth },
			{ "x", EffectiveBorderWidth / 2 },
			{ "y", EffectiveBorderWidth / 2 },
			{ "rx", CornerRadius },
			{ "ry", CornerRadius }
		};

	/// <summary>
	/// Builds SVG attributes for the switch text.
	/// </summary>
	/// <returns>Attribute dictionary.</returns>
	public IDictionary<string, object> GetTextAttributes() => new Dictionary<string, object>
		{
			{ "class", $"text {StateCssClass} {TextCssClass ?? Options.TextCssClass}".TrimEnd()},
			{ "text-anchor",  Value ? "start" : "end" },
			{ "x", Value ? EffectiveBorderWidth * 3 : CalculatedWidth - EffectiveBorderWidth * 3 },
			{ "y", InnerHeight / 2 + (InnerHeight / 2) + TextYOffset }
		};

	/// <summary>
	/// Builds SVG attributes for the toggle thumb.
	/// </summary>
	/// <returns>Attribute dictionary.</returns>
	public IDictionary<string, object> GetToggleAttributes() => new Dictionary<string, object>
		{
			{ "class", $"toggle {StateCssClass}"},
			{ "height", InnerHeight},
			{ "width", CalculatedHeight - EffectiveBorderWidth - 2},
			{ "x", Value ? CalculatedWidth - CalculatedHeight + EffectiveBorderWidth - 1 : EffectiveBorderWidth + 1 },
			{ "y", EffectiveBorderWidth + 1 },
			{ "rx", CornerRadius },
			{ "ry", CornerRadius }
		};

	/// <summary>
	/// Loads JS module and performs first-render measurements.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDToggleSwitch.razor.js").ConfigureAwait(true);
				await RefreshTextWidthAsync().ConfigureAwait(true);
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	/// <summary>
	/// Refreshes derived text width when parameters change.
	/// </summary>
	/// <returns>A refresh task.</returns>
	protected override async Task OnParametersSetAsync() => await RefreshTextWidthAsync().ConfigureAwait(true);

	private async Task OnClickAsync()
	{
		if (IsEnabled)
		{
			Value = !Value;
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}
	}

	private async Task OnKeyPressAsync(KeyboardEventArgs args)
	{
		if (IsEnabled && (args.Code == "Space" || args.Code == "Enter"))
		{
			Value = !Value;
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Measures on/off text and updates rendered switch width when required.
	/// </summary>
	/// <returns>A measurement task.</returns>
	protected async Task RefreshTextWidthAsync()
	{
		try
		{
			if (_module != null)
			{
				var fontSize = (Size ?? Options.Size) switch
				{
					ButtonSizes.Small => "0.5rem",
					ButtonSizes.Large => "1.5rem",
					_ => "1rem"
				};
				var onWidth = await MeasureTextAsync(_module, OnText ?? Options.OnText, fontSize).ConfigureAwait(true);
				var offWidth = await MeasureTextAsync(_module, OffText ?? Options.OffText, fontSize).ConfigureAwait(true);
				var newWidth = Math.Max(onWidth, offWidth);
				if (newWidth > _textWidth)
				{
					_textWidth = newWidth;
					StateHasChanged();
				}
			}
		}
		catch (ObjectDisposedException)
		{
			// ignore object disposed exception
		}
	}

	private static async Task<double> MeasureTextAsync(IJSObjectReference module, string? text, string fontSize)
		=> string.IsNullOrEmpty(text) ? 0 : await module.InvokeAsync<double>("measureText", text, fontSize).ConfigureAwait(true);
}