using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor;

public partial class PDAudioButton : PDAudioControl
{
    [Parameter] public string ActiveColor { get; set; } = "#00ff00";
    [Parameter] public string InactiveColor { get; set; } = "#444";

    protected override string JsFileName => string.Empty;

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
