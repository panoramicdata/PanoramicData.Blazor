namespace PanoramicData.Blazor;

/// <summary>
/// The left panel's width, which a host may let the user change by dragging the panel's edge, or with the arrow keys
/// once the edge has focus.
/// </summary>
public partial class PDStudio
{
	private ElementReference _leftPanelHandle;
	private string? _initializedLeftPanelHandleId;
	private IJSObjectReference? _leftPanelModule;
	private DotNetObjectReference<PDStudio>? _leftPanelReference;
	private double? _leftPanelWidth;
	private double? _leftPanelWidthGiven;

	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets whether the user can resize the left panel by dragging its edge, or with the arrow keys once the
	/// edge has focus; double-clicking the edge gives the panel back its usual width.
	/// </summary>
	[Parameter] public bool LeftPanelResizable { get; set; }

	/// <summary>
	/// Gets or sets the left panel's width in rem, kept within <see cref="LeftPanelMinWidth"/> and
	/// <see cref="LeftPanelMaxWidth"/>; null leaves it to the stylesheet's --pd-studio-left-panel-width.
	/// </summary>
	[Parameter] public double? LeftPanelWidth { get; set; }

	/// <summary>
	/// Raised with the left panel's new width in rem when the user resizes it, or null when they give it back its
	/// usual width, so a host can remember it.
	/// </summary>
	[Parameter] public EventCallback<double?> LeftPanelWidthChanged { get; set; }

	/// <summary>Gets or sets the narrowest, in rem, that the user can make the left panel.</summary>
	[Parameter] public double LeftPanelMinWidth { get; set; } = 10;

	/// <summary>Gets or sets the widest, in rem, that the user can make the left panel.</summary>
	[Parameter] public double LeftPanelMaxWidth { get; set; } = 40;

	private bool ShowsLeftPanelHandle => LeftPanelResizable && LeftPanelContent != null;

	private string LeftPanelCssClass => ShowsLeftPanelHandle ? "pd-studio-left-panel pd-studio-left-panel-resizable" : "pd-studio-left-panel";

	// The width is a custom property, so the stylesheet still decides how the panel uses it
	private string? LeftPanelStyle => _leftPanelWidth is { } width ? $"--pd-studio-left-panel-width: {Rem(width)}rem" : null;

	private string? LeftPanelWidthText => _leftPanelWidth is { } width ? Rem(width) : null;

	private static string Rem(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

	/// <summary>
	/// Called by the left panel's edge when the user has dragged it, or moved it with the keyboard, to a width in rem.
	/// </summary>
	/// <param name="width">The panel's new width in rem.</param>
	[JSInvokable]
	public Task OnLeftPanelResized(double width)
		=> double.IsFinite(width) ? SetLeftPanelWidthAsync(ClampLeftPanelWidth(width)) : Task.CompletedTask;

	private Task ResetLeftPanelWidthAsync() => SetLeftPanelWidthAsync(null);

	// Run from a double-click or the script, where a host handler's failure would otherwise end the circuit
	private async Task SetLeftPanelWidthAsync(double? width)
	{
		_leftPanelWidth = width;
		await RunSafelyAsync(() => LeftPanelWidthChanged.InvokeAsync(width), "report the left panel's width");
		StateHasChanged();
	}

	private double ClampLeftPanelWidth(double width) => Math.Max(LeftPanelMinWidth, Math.Min(width, LeftPanelMaxWidth));

	// A width the host gives is taken when it changes, so one the user dragged to stays until the host gives another
	private void TakeLeftPanelWidth()
	{
		if (Nullable.Equals(LeftPanelWidth, _leftPanelWidthGiven))
		{
			return;
		}

		_leftPanelWidthGiven = LeftPanelWidth;
		_leftPanelWidth = LeftPanelWidth is { } width && double.IsFinite(width) ? ClampLeftPanelWidth(width) : null;
	}

	// Once for each edge drawn, as hiding the edge, such as when the host closes its panel, removes it
	private async Task InitializeLeftPanelHandleAsync()
	{
		if (!ShowsLeftPanelHandle || _leftPanelHandle.Id is not { } handleId || handleId == _initializedLeftPanelHandleId)
		{
			return;
		}

		_initializedLeftPanelHandleId = handleId;
		try
		{
			_leftPanelModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDStudio.razor.js").ConfigureAwait(true);
			_leftPanelReference ??= DotNetObjectReference.Create(this);
			if (_leftPanelModule != null)
			{
				await _leftPanelModule.InvokeVoidAsync("initializeLeftPanelHandle", _leftPanelHandle, _leftPanelReference).ConfigureAwait(true);
			}
		}
		catch (JSDisconnectedException)
		{
			// The page has gone
		}
		catch (Exception ex)
		{
			// Thrown from after a render it would end the circuit; the edge then only resets on a double-click
			Logger.LogWarning(ex, "PDStudio could not make its left panel resizable");
		}
	}

	private void DisposeLeftPanelInterop()
	{
		_leftPanelReference?.Dispose();
		if (_leftPanelModule is { } module)
		{
			_ = DisposeLeftPanelModuleAsync(module);
		}
	}

	private async Task DisposeLeftPanelModuleAsync(IJSObjectReference module)
	{
		try
		{
			await module.DisposeAsync().ConfigureAwait(false);
		}
		catch (JSDisconnectedException)
		{
			// The page has gone, and the module with it
		}
		catch (Exception ex)
		{
			// Nothing awaits this, so a failure is only logged
			Logger.LogDebug(ex, "PDStudio could not release its left panel's script");
		}
	}
}
