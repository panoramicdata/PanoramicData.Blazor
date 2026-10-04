namespace PanoramicData.Blazor;

/// <summary>
/// PDAudioControl: changing the value by dragging the pointer, and resetting it by double-clicking.
/// </summary>
public abstract partial class PDAudioControl
{
	private bool _isDragging;
	private double _dragOriginValue;
	private double _dragOriginY;
	private DotNetObjectReference<PDAudioControl>? _dotNetRef;
	private IJSObjectReference? _jsModule;

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
}
