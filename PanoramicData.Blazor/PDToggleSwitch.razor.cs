using PanoramicData.Blazor.Options;

namespace PanoramicData.Blazor;

public partial class PDToggleSwitch : IAsyncDisposable
{
	private static int _sequence;

	private double _textWidth;
	private IJSObjectReference? _module;
	private readonly string _textCache = string.Empty;

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter] public int? BorderWidth { get; set; }

	[Parameter] public int? Height { get; set; }

	[Parameter] public string Id { get; set; } = $"pd-toggleswitch-{++_sequence}";

	[Parameter] public string Label { get; set; } = string.Empty;

	[Parameter] public bool? LabelBefore { get; set; }

	[Parameter] public string? OffText { get; set; }

	[Parameter] public string? OnText { get; set; }

	[Parameter] public PDToggleSwitchOptions Options { get; set; } = new();

	[Parameter] public bool? Rounded { get; set; }

	[Parameter] public bool Value { get; set; }

	[Parameter] public EventCallback<bool> ValueChanged { get; set; }

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
		ButtonSizes.Small => 32 + (_textWidth > 8 ? _textWidth - 8 : _textWidth),
		ButtonSizes.Large => 64 + (_textWidth > 24 ? _textWidth - 24 : _textWidth),
		_ => 48 + (_textWidth > 16 ? _textWidth - 16 : _textWidth),
	};

	private double InnerHeight => CalculatedHeight - 2 - (BorderWidth ?? Options.BorderWidth) * 2;

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
		}
	}

	public IDictionary<string, object> GetBackgroundAttributes() => new Dictionary<string, object>
		{
			{ "class", $"switch {(Value ? "on" : "off")}"},
			{ "height", CalculatedHeight - (BorderWidth ?? Options.BorderWidth)},
			{ "width", CalculatedWidth - (BorderWidth ?? Options.BorderWidth) },
			{ "x", (BorderWidth ?? Options.BorderWidth) / 2 },
			{ "y", (BorderWidth ?? Options.BorderWidth) / 2 },
			{ "rx", (Rounded ?? Options.Rounded) ? CalculatedHeight / 2 : 0 },
			{ "ry", (Rounded ?? Options.Rounded) ? CalculatedHeight / 2 : 0 }
		};

	public IDictionary<string, object> GetTextAttributes() => new Dictionary<string, object>
		{
			{ "class", $"text {(Value ? "on" : "off")}"},
			{ "text-anchor",  Value ? "start" : "end" },
			{ "x", Value ? (BorderWidth ?? Options.BorderWidth) * 3 : CalculatedWidth - (BorderWidth ?? Options.BorderWidth) * 3 },
			{ "y", InnerHeight / 2 + (InnerHeight / 2) + TextYOffset }
		};

	public IDictionary<string, object> GetToggleAttributes() => new Dictionary<string, object>
		{
			{ "class", $"toggle {(Value ? "on" : "off")}"},
			{ "height", InnerHeight},
			{ "width", CalculatedHeight - (BorderWidth ?? Options.BorderWidth) - 2},
			{ "x", Value ? CalculatedWidth - CalculatedHeight + (BorderWidth ?? Options.BorderWidth) - 1 : (BorderWidth ?? Options.BorderWidth) + 1 },
			{ "y", (BorderWidth ?? Options.BorderWidth) + 1 },
			{ "rx", Rounded ?? Options.Rounded ? CalculatedHeight / 2 : 0 },
			{ "ry", Rounded ?? Options.Rounded ? CalculatedHeight / 2 : 0 }
		};

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDToggleSwitch.razor.js").ConfigureAwait(true);
			await RefreshTextWidthAsync().ConfigureAwait(true);
		}
	}

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
				var onText = OnText ?? Options.OnText;
				var offText = OffText ?? Options.OffText;
				var onWidth = string.IsNullOrEmpty(onText) ? 0 : await _module.InvokeAsync<double>("measureText", onText, fontSize).ConfigureAwait(true);
				var offWidth = string.IsNullOrEmpty(offText) ? 0 : await _module.InvokeAsync<double>("measureText", offText, fontSize).ConfigureAwait(true);
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
}