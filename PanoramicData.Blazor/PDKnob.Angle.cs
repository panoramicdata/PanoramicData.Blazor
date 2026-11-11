namespace PanoramicData.Blazor;

/// <summary>
/// PDKnob: the <see cref="PDKnobMode.Angle"/> mode, a full-circle dial that turns past 360 degrees.
/// </summary>
public partial class PDKnob
{
	private static readonly double _fullTurn = 360;
	private static readonly double _tolerance = 1e-9;

	private bool _isAngleDragging;
	private double _angleRaw;
	private double _anglePointerLast;

	/// <summary>
	/// Gets or sets the value in degrees when <see cref="Mode"/> is <see cref="PDKnobMode.Angle"/>.
	/// </summary>
	[Parameter] public double Angle { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when <see cref="Angle"/> changes.
	/// </summary>
	[Parameter] public EventCallback<double> AngleChanged { get; set; }

	/// <summary>
	/// Gets or sets the direction the indicator points at an angle of 0, in degrees clockwise from pointing right.
	/// </summary>
	[Parameter] public double AngleZeroDirection { get; set; }

	/// <summary>
	/// Gets or sets whether the angle increases clockwise (the default, as SVG and CSS angles do) or anticlockwise.
	/// </summary>
	[Parameter] public bool AngleClockwise { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the reported angle is normalised to 0 up to (but not including) 360 (the default),
	/// or kept as an unbounded running total, so that 370 is distinct from 10.
	/// </summary>
	[Parameter] public bool AngleWrap { get; set; } = true;

	/// <summary>
	/// Gets or sets the step, in degrees, that the angle snaps to while dragging and that the arrow keys move by.
	/// 0 (the default) means no snapping, and the arrow keys move by 1 degree.
	/// Hold Shift while dragging to move freely.
	/// </summary>
	[Parameter] public double AngleStep { get; set; }

	/// <summary>
	/// Gets or sets the step, in degrees, that Page Up and Page Down move by.
	/// </summary>
	[Parameter] public double AngleLargeStep { get; set; } = 15;

	/// <summary>
	/// Gets or sets the angle a double-click resets to.
	/// </summary>
	[Parameter] public double DefaultAngle { get; set; }

	/// <summary>
	/// Gets or sets the accessible name of an angle knob. When not set, <see cref="PDAudioControl.Label"/> is used.
	/// </summary>
	[Parameter] public string? AriaLabel { get; set; }

	private bool IsAngleMode => Mode == PDKnobMode.Angle;

	/// <summary>
	/// Gets the indicator direction for the angle mode, in the dial's own convention (degrees clockwise from up).
	/// </summary>
	private double AngleIndicatorDirection => 90 + AngleZeroDirection + (AngleClockwise ? Angle : -Angle);

	/// <summary>
	/// Gets the accessibility attributes, present only in the angle mode so other modes render as before.
	/// </summary>
	private Dictionary<string, object> AngleAttributes
	{
		get
		{
			if (!IsAngleMode)
			{
				return [];
			}

			var value = Angle.ToString("0.##", CultureInfo.InvariantCulture);
			var attributes = new Dictionary<string, object>
			{
				["role"] = "slider",
				["tabindex"] = IsEnabled ? "0" : "-1",
				["aria-label"] = AriaLabel ?? Label ?? "Angle",
				["aria-valuenow"] = value,
				["aria-valuetext"] = $"{value} degrees",
				["aria-disabled"] = IsEnabled ? "false" : "true"
			};
			if (AngleWrap)
			{
				attributes["aria-valuemin"] = "0";
				attributes["aria-valuemax"] = "360";
			}

			return attributes;
		}
	}

	/// <summary>
	/// Normalises an angle to 0 up to (but not including) 360 degrees.
	/// </summary>
	/// <param name="angle">The angle in degrees.</param>
	/// <returns>The equivalent angle in the range 0 to 360.</returns>
	public static double NormaliseAngle(double angle)
	{
		var result = angle % _fullTurn;
		if (result < 0)
		{
			result += _fullTurn;
		}

		return result >= _fullTurn - _tolerance || Math.Abs(result) < _tolerance ? 0 : result;
	}

	/// <summary>
	/// Wraps a change of angle into the range -180 to 180, the shortest way round.
	/// </summary>
	private static double ShortestTurn(double delta)
	{
		var result = NormaliseAngle(delta);
		return result > 180 ? result - _fullTurn : result;
	}

	/// <summary>
	/// Gets the screen direction of a point, in degrees clockwise from pointing right.
	/// </summary>
	private static double ScreenDirection(double x, double y) => Math.Atan2(y, x) * 180 / Math.PI;

	private double SnapAngle(double angle, bool free)
		=> AngleStep > 0 && !free ? Math.Round(angle / AngleStep) * AngleStep : angle;

	private double ReportedAngle(double angle) => AngleWrap ? NormaliseAngle(angle) : angle;

	private async Task SetAngleAsync(double angle)
	{
		var reported = ReportedAngle(angle);
		if (Math.Abs(reported - Angle) > _tolerance)
		{
			Angle = reported;
			await AngleChanged.InvokeAsync(reported).ConfigureAwait(true);
		}
	}

	private async Task OnKnobPointerDownAsync(PointerEventArgs e)
	{
		if (!IsAngleMode)
		{
			await OnPointerDown(e).ConfigureAwait(true);
			return;
		}

		if (!IsEnabled || _isAngleDragging)
		{
			return;
		}

		var module = await GetJsModuleAsync().ConfigureAwait(true);
		if (module is null)
		{
			return;
		}

		double[]? start;
		try
		{
			start = await module.InvokeAsync<double[]?>("registerAngleEvents", DotNetReference, _svgRef, e.ClientX, e.ClientY).ConfigureAwait(true);
		}
		catch (JSDisconnectedException)
		{
			// The circuit is gone, so there is nothing left to do.
			return;
		}
		if (start is { Length: 2 })
		{
			_isAngleDragging = true;
			_angleRaw = Angle;
			_anglePointerLast = ScreenDirection(start[0], start[1]);
		}
	}

	/// <summary>
	/// Turns the dial as the pointer moves round its centre while dragging in the angle mode.
	/// </summary>
	/// <param name="x">Pointer X offset from the dial's centre, in pixels.</param>
	/// <param name="y">Pointer Y offset from the dial's centre, in pixels, increasing downwards.</param>
	/// <param name="free">Whether the snapping modifier (Shift) is held, so the angle moves freely.</param>
	/// <returns>An update task.</returns>
	[JSInvokable]
	public async Task OnAnglePointerMove(double x, double y, bool free)
	{
		if (!_isAngleDragging)
		{
			return;
		}

		var direction = ScreenDirection(x, y);
		var turn = ShortestTurn(direction - _anglePointerLast);
		_anglePointerLast = direction;
		_angleRaw += AngleClockwise ? turn : -turn;
		await SetAngleAsync(SnapAngle(_angleRaw, free)).ConfigureAwait(true);
	}

	/// <summary>
	/// Ends an angle-mode drag.
	/// </summary>
	[JSInvokable]
	public void OnAnglePointerUp() => _isAngleDragging = false;

	private async Task OnKnobKeyDownAsync(KeyboardEventArgs e)
	{
		if (!IsAngleMode || !IsEnabled)
		{
			return;
		}

		var step = AngleStep > 0 ? AngleStep : 1;
		var delta = e.Key switch
		{
			"ArrowUp" or "ArrowRight" => step,
			"ArrowDown" or "ArrowLeft" => -step,
			"PageUp" => AngleLargeStep,
			"PageDown" => -AngleLargeStep,
			_ => 0
		};
		if (Math.Abs(delta) > _tolerance)
		{
			await SetAngleAsync(SnapAngle(Angle + delta, false)).ConfigureAwait(true);
		}
	}

	private async Task OnKnobDoubleClickAsync()
	{
		if (!IsAngleMode)
		{
			OnDoubleClick();
			return;
		}

		if (IsEnabled)
		{
			await SetAngleAsync(DefaultAngle).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Draws a tick every 45 degrees round the angle dial, starting at the zero direction.
	/// </summary>
	private List<Tick> GetAngleTicks()
	{
		var ticks = new List<Tick>();
		for (var i = 0; i < 8; i++)
		{
			var direction = 90 + AngleZeroDirection + i * 45;
			var (x1, y1) = PolarToCartesian(Center, Center, Radius + 2, direction);
			var (x2, y2) = PolarToCartesian(Center, Center, Radius + (i == 0 ? 8 : 5), direction);
			ticks.Add(new Tick(x1, y1, x2, y2));
		}

		return ticks;
	}
}
