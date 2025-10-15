using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDAudioPadPage
{
	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void HandlePadValueChanged(PDAudioPadEventArgs args)
	{
		var eventMessage = args.DecayMode == DecayMode.Toggle
			? $"{args.Label ?? "Pad"}: {(args.IsActive ? "ON" : "OFF")}"
			: $"{args.Label ?? "Pad"}: {args.Value:F3}";

		EventManager?.Add(new Event("PadValueChanged", new EventArgument("Value", eventMessage)));
	}
}