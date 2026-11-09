namespace PanoramicData.Blazor;

/// <summary>
/// Shows a message that fails to render as a short notice instead of ending the circuit, and tries it again each time
/// the list of messages renders.
/// </summary>
/// <remarks>Recovering resets the error count, so a message that keeps failing never reaches the boundary's limit.</remarks>
public sealed class PDMessageErrorBoundary : ErrorBoundary
{
	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		if (CurrentException is not null)
		{
			Recover();
		}

		base.OnParametersSet();
	}
}
