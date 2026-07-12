using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor;

/// <summary>
/// A toggle button component that represents an on/off audio control with configurable active and inactive colors.
/// </summary>
public partial class PDAudioButton : PDAudioControl
{
    /// <summary>Gets or sets the color displayed when the button is in the active (on) state.</summary>
    [Parameter] public string ActiveColor { get; set; } = "#00ff00";
    /// <summary>Gets or sets the color displayed when the button is in the inactive (off) state.</summary>
    [Parameter] public string InactiveColor { get; set; } = "#444";

	/// <inheritdoc />
    protected override string JsFileName => string.Empty;

    /// <summary>
    /// Toggles the button between the active (on) and inactive (off) state and raises the <see cref="PDAudioControl.ValueChanged"/> callback.
    /// </summary>
    protected async Task Toggle()
    {
        if (!IsEnabled)
        {
            return;
        }

        Value = Value > 0.5 ? 0 : 1;
        await ValueChanged.InvokeAsync(Value);
    }
}
