using PanoramicData.Blazor.Enums;

namespace PanoramicData.Blazor;

/// <summary>
/// Base class for interactive audio controls with drag, snap, and label behavior.
/// </summary>
public abstract class PDAudioControl : ComponentBase, IAsyncDisposable
{
	/// <summary>
	/// Gets or sets current normalized value in the range 0..1.
	/// </summary>
	[Parameter] public double Value { get; set; } = 0.5;

	/// <summary>
	/// Gets or sets callback invoked when <see cref="Value"/> changes.
	/// </summary>
	[Parameter] public EventCallback<double> ValueChanged { get; set; }

	/// <summary>
	/// Gets or sets value used when resetting via double-click.
	/// </summary>
	[Parameter] public double? DefaultValue { get; set; }

	/// <summary>
	/// Gets or sets whether control interaction is enabled.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets current snap increment for value quantization.
	/// </summary>
	[Parameter] public double SnapIncrement { get; set; }

	/// <summary>
	/// Gets or sets number of snap points used to quantize control values.
	/// </summary>
	[Parameter] public int? SnapPoints { get; set; }

	/// <summary>
	/// Gets or sets optional label text.
	/// </summary>
	[Parameter] public string? Label { get; set; }

	/// <summary>
	/// Gets or sets label container height in pixels.
	/// </summary>
	[Parameter] public int LabelHeightPx { get; set; } = 20;

	/// <summary>
	/// Gets or sets optional CSS class for label styling.
	/// </summary>
	[Parameter] public string? LabelCssClass { get; set; }

	/// <summary>
	/// Gets or sets label position relative to the control.
	/// </summary>
	[Parameter] public PDLabelPosition LabelPosition { get; set; } = PDLabelPosition.Below;

	/// <summary>
	/// Gets or sets optional CSS class applied to the control container.
	/// </summary>
	[Parameter] public string? CssClass { get; set; } // Allow user to override CSS

	/// <summary>
	/// Gets or sets JavaScript runtime used by this control.
	/// </summary>
	[Inject] protected IJSRuntime JS { get; set; } = default!;

	/// <summary>
	/// Gets or sets logger used by audio controls.
	/// </summary>
	[Inject] protected ILogger<PDAudioControl> Logger { get; set; } = default!;

	private bool _isDragging;
	private double _dragOriginValue;
	private double _dragOriginY;
	private DotNetObjectReference<PDAudioControl>? _dotNetRef;
	private IJSObjectReference? _jsModule;
	private int? _previousSnapPoints;
	/// <summary>
	/// Gets the JavaScript module path used to register pointer events.
	/// </summary>
	protected virtual string JsFileName => string.Empty;

	/// <summary>
	/// Applies parameter-driven defaults and snap-point behavior.
	/// </summary>
	/// <returns>An update task.</returns>
	protected override async Task OnParametersSetAsync()
	{
		DefaultValue ??= 0.5;
		
		// Update SnapIncrement based on SnapPoints
		if (SnapPoints > 1)
		{
			SnapIncrement = 1.0 / (SnapPoints.Value - 1);
		}
		else
		{
			SnapIncrement = 0;
		}

		// If SnapPoints changed, notify parent to snap the value
		if (_previousSnapPoints != SnapPoints && _previousSnapPoints != null)
		{
			_previousSnapPoints = SnapPoints;
			
			if (SnapIncrement > 0)
			{
				var clampedValue = Math.Clamp(Value, 0, 1);
				var newValue = Math.Round(clampedValue / SnapIncrement) * SnapIncrement;
				if (Math.Abs(newValue - Value) > 0.0001)
				{
					await ValueChanged.InvokeAsync(newValue);
				}
			}
		}
		else
		{
			_previousSnapPoints ??= SnapPoints;
		}

		await base.OnParametersSetAsync();
	}

	/// <summary>
	/// Handles pointer-down and registers drag listeners via JavaScript.
	/// </summary>
	/// <param name="e">Pointer event args.</param>
	/// <returns>A registration task.</returns>
	protected async Task OnPointerDown(PointerEventArgs e)
	{
		if (!IsEnabled || _isDragging)
		{
			return;
		}

		if (!string.IsNullOrEmpty(JsFileName))
		{
			_isDragging = true;
			_dragOriginY = e.ClientY;
			_dragOriginValue = Value;

			_dotNetRef ??= DotNetObjectReference.Create(this);
			_jsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", JsFileName);
			
			await _jsModule.InvokeVoidAsync("registerAudioControlEvents", _dotNetRef);
		}
	}

	/// <summary>
	/// Handles pointer move updates from JavaScript while dragging.
	/// </summary>
	/// <param name="clientY">Current client Y position.</param>
	/// <returns>An update task.</returns>
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

		if (Math.Abs(newValue - Value) > 0.0001)
		{
			await ValueChanged.InvokeAsync(newValue);
		}
	}

	/// <summary>
	/// Handles pointer-up event from JavaScript and ends dragging.
	/// </summary>
	/// <param name="clientY">Current client Y position.</param>
	[JSInvokable]
	public void OnPointerUp(double clientY)
	{
		_isDragging = false;
	}

	/// <summary>
	/// Resets value to default when double-clicked.
	/// </summary>
	protected async void OnDoubleClick()
	{
		var newValue = DefaultValue ?? 0.5;
		if (SnapIncrement > 0)
		{
			newValue = Math.Round(newValue / SnapIncrement) * SnapIncrement;
		}

		await ValueChanged.InvokeAsync(newValue);
	}

	/// <summary>
	/// Calculates a readable marking step for a value range.
	/// </summary>
	/// <param name="maxVolume">Maximum value in the range.</param>
	/// <returns>Suggested step interval.</returns>
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
	/// <summary>
	/// Renders the control label when one is configured.
	/// </summary>
	/// <returns>Label fragment.</returns>
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

	/// <summary>
	/// Disposes JavaScript resources used by this control.
	/// </summary>
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
