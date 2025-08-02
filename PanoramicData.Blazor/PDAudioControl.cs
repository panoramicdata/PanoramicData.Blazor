using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

public abstract class PDAudioControl : ComponentBase, IAsyncDisposable
{
	[Parameter] public double Value { get; set; } = 0.5;

	[Parameter] public EventCallback<double> ValueChanged { get; set; }

	[Parameter] public double? DefaultValue { get; set; }

	[Parameter] public bool IsEnabled { get; set; } = true;

	[Parameter] public double SnapIncrement { get; set; }

	[Parameter] public int? SnapPoints { get; set; }

	[Parameter] public string? Label { get; set; }

	[Parameter] public int LabelHeightPx { get; set; } = 20;

	[Parameter] public string? LabelCssClass { get; set; }

	[Parameter] public PDLabelPosition LabelPosition { get; set; } = PDLabelPosition.Below;

	[Parameter] public string? CssClass { get; set; } // Allow user to override CSS

	[Inject] protected IJSRuntime JS { get; set; } = default!;

	[Inject] protected ILogger<PDAudioControl> Logger { get; set; } = default!;

	private bool _isDragging;
	private double _dragOriginValue;
	private double _dragOriginY;
	private DotNetObjectReference<PDAudioControl>? _dotNetRef;
	private IJSObjectReference? _jsModule;
	protected virtual string JsFileName => string.Empty; // Make virtual instead of abstract

	protected override void OnParametersSet()
	{
		DefaultValue ??= 0.5;
		Value = Math.Clamp(Value, 0, 1);
		if (SnapPoints > 1)
		{
			SnapIncrement = 1.0 / (SnapPoints.Value - 1);
		}

		if (SnapIncrement > 0)
		{
			Value = Math.Round(Value / SnapIncrement) * SnapIncrement;
		}
	}

	protected async Task OnPointerDown(PointerEventArgs e)
	{
		if (!IsEnabled)
		{
			return;
		}

		// Only import JS module if JsFileName is not empty
		if (!string.IsNullOrEmpty(JsFileName))
		{
			Logger.LogInformation("OnPointerDown: ClientY={ClientY}, Value={Value}", e.ClientY, Value);
			_isDragging = true;
			_dragOriginY = e.ClientY;
			_dragOriginValue = Value;

			_dotNetRef ??= DotNetObjectReference.Create(this);
			_jsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", JsFileName);
			await _jsModule.InvokeVoidAsync("registerAudioControlEvents", _dotNetRef);
		}
	}

	[JSInvokable]
	public async Task OnPointerMove(double clientY)
	{
		if (!_isDragging)
		{
			return;
		}

		var deltaY = _dragOriginY - clientY;
		var sensitivity = 150.0;
		var newValue = _dragOriginValue + (deltaY / sensitivity);
		newValue = Math.Clamp(newValue, 0, 1);
		if (SnapIncrement > 0)
		{
			newValue = Math.Round(newValue / SnapIncrement) * SnapIncrement;
		}

		if (newValue != Value)
		{
			Value = newValue;
			await ValueChanged.InvokeAsync(Value);
			StateHasChanged();
		}
	}

	[JSInvokable]
	public void OnPointerUp(double clientY)
	{
		Logger.LogInformation(
			"OnPointerUp: ClientY={ClientY}, Value={Value}",
			clientY,
			Value);
		_isDragging = false;
	}

	protected async void OnDoubleClick()
	{
		Logger.LogInformation("OnDoubleClick: Resetting to DefaultValue={DefaultValue}", DefaultValue);
		Value = DefaultValue ?? 0.5;
		if (SnapIncrement > 0)
		{
			Value = Math.Round(Value / SnapIncrement) * SnapIncrement;
		}

		await ValueChanged.InvokeAsync(Value);
		StateHasChanged();
	}

	protected static int CalculateMarkingStep(int maxVolume)
	{
		if (maxVolume <= 12)
		{
			return 1;
		}

		double targetMarks = 10.0;
		double rawStep = maxVolume / targetMarks;

		return rawStep switch
		{
			<= 1 => 1,
			<= 2 => 2,
			<= 5 => 5,
			<= 10 => 10,
			<= 20 => 20,
			<= 50 => 50,
			<= 100 => 100,
			_ => (int)Math.Ceiling(rawStep / 10) * 10
		};
	}

	// Render label above or below
	protected RenderFragment RenderLabel() => builder =>
	{
		if (!string.IsNullOrEmpty(Label))
		{
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "class", $"pd-audio-label {LabelCssClass}");
			builder.AddAttribute(2, "style", $"height:{LabelHeightPx}px;text-align:center;pointer-events:none;");
			builder.AddContent(3, Label);
			builder.CloseElement();
		}
	};

	public async ValueTask DisposeAsync()
	{
		_dotNetRef?.Dispose();
		if (_jsModule is not null)
		{
			await _jsModule.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}
}
