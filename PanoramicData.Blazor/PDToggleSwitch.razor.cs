using PanoramicData.Blazor.Options;

namespace PanoramicData.Blazor;

public partial class PDToggleSwitch : IAsyncDisposable
{
	private static int _sequence;

	private double _textWidth;
	private IJSObjectReference? _module;
	private string _textCache = string.Empty;

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter] public string? BorderColour { get; set; }

	[Parameter] public int? BorderWidth { get; set; }

	[Parameter] public int? Height { get; set; }

	[Parameter] public string Id { get; set; } = $"pd-toggleswitch-{++_sequence}";

	[Parameter] public string? OffBackgroundColour { get; set; }

	[Parameter] public string? OffForegroundColour { get; set; }

	[Parameter] public string? OffText { get; set; }

	[Parameter] public string? OnBackgroundColour { get; set; }

	[Parameter] public string? OnForegroundColour { get; set; }

	[Parameter] public string? OnText { get; set; }

	[Parameter] public PDToggleSwitchOptions Options { get; set; } = new();

	[Parameter] public bool? Rounded { get; set; }

	[Parameter] public string? ToggleColour { get; set; }

	[Parameter] public bool Value { get; set; }

	[Parameter] public EventCallback<bool> ValueChanged { get; set; }

	[Parameter] public int? Width { get; set; }

	#region Helper Properties

	private double CalculatedHeight => Height ?? Options.Height ?? Size switch
	{
		ButtonSizes.Small => 16,
		ButtonSizes.Large => 32,
		_ => 24
	};

	private double CalculatedWidth => Width ?? Options.Width ?? Size switch
	{
		ButtonSizes.Small => 32,
		ButtonSizes.Large => 64,
		_ => 48
	};

	private string InnerColour => Value
		? (string.IsNullOrWhiteSpace(OnBackgroundColour ?? Options.OnBackgroundColour)
			? BorderColour ?? Options.BorderColour
			: OnBackgroundColour ?? Options.OnBackgroundColour)
		: OffBackgroundColour ?? Options.OffBackgroundColour;

	private double InnerHeight => CalculatedHeight - 2 - (BorderWidth ?? Options.BorderWidth) * 2;

	private string TextColour => Value
		? (string.IsNullOrWhiteSpace(OnForegroundColour ?? Options.OnForegroundColour)
			? ToggleColour ?? Options.ToggleColour
			: OnForegroundColour ?? Options.OnForegroundColour!)
		: (string.IsNullOrWhiteSpace(OffForegroundColour ?? Options.OffForegroundColour)
			? ToggleColour ?? Options.ToggleColour
			: OffForegroundColour ?? Options.OffForegroundColour!);

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

	public IDictionary<string, object> GetBackgroundAttributes()
	{
		return new Dictionary<string, object>
		{
			{ "height", CalculatedHeight - (BorderWidth ?? Options.BorderWidth)},
			{ "style", $"fill: {InnerColour}; stroke: {BorderColour ?? Options.BorderColour}; stroke-width: {BorderWidth ?? Options.BorderWidth}" },
			{ "width", CalculatedWidth - (BorderWidth ?? Options.BorderWidth) },
			{ "x", (BorderWidth ?? Options.BorderWidth) / 2 },
			{ "y", (BorderWidth ?? Options.BorderWidth) / 2 },
			{ "rx", (Rounded ?? Options.Rounded) ? CalculatedHeight / 2 : 0 },
			{ "ry", (Rounded ?? Options.Rounded) ? CalculatedHeight / 2 : 0 }
		};
	}

	public IDictionary<string, object> GetTextAttributes()
	{
		return new Dictionary<string, object>
		{
			{ "style", $"font-size: {InnerHeight / 1.5}px; stroke: {TextColour}; fill: {TextColour}" },
			{ "text-anchor",  Value ? "start" : "end" },
			{ "x", Value ? (BorderWidth ?? Options.BorderWidth) * 5 : CalculatedWidth - (BorderWidth ?? Options.BorderWidth) * 5 },
			{ "y", InnerHeight / 2 + (InnerHeight / 3) }
		};
	}

	public IDictionary<string, object> GetToggleAttributes()
	{
		return new Dictionary<string, object>
		{
			{ "height", InnerHeight},
			{ "style", $"fill: {ToggleColour ?? Options.ToggleColour}; stroke: {ToggleColour ?? Options.ToggleColour}" },
			{ "width", CalculatedHeight - (BorderWidth ?? Options.BorderWidth) - 2},
			{ "x", Value ? CalculatedWidth - CalculatedHeight + (BorderWidth ?? Options.BorderWidth) - 1 : (BorderWidth ?? Options.BorderWidth) + 1 },
			{ "y", (BorderWidth ?? Options.BorderWidth) + 1 },
			{ "rx", Rounded ?? Options.Rounded ? CalculatedHeight / 2 : 0 },
			{ "ry", Rounded ?? Options.Rounded ? CalculatedHeight / 2 : 0 }
		};
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDToggleSwitch.razor.js").ConfigureAwait(true);
			await RefreshTextWidthAsync().ConfigureAwait(true);
		}
	}

	protected override Task OnParametersSetAsync()
	{
		return RefreshTextWidthAsync();
	}

	private async Task OnClickAsync()
	{
		if (IsEnabled)
		{
			Value = !Value;
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}
	}

	protected Task RefreshTextWidthAsync()
	{
		var text = OnText + OffText;
		if (_module != null && _textCache != text)
		{
			_textWidth = 30;
			//_textWidth = await _module.InvokeAsync<double>("measureText", Id, OnText, OffText).ConfigureAwait(true);
			_textCache = text;
		}
		return Task.CompletedTask;
	}
}