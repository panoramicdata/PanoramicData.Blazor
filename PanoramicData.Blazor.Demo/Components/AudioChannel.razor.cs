namespace PanoramicData.Blazor.Demo.Components;
public partial class AudioChannel
{
	[Parameter] public string Name { get; set; } = string.Empty;

	[Parameter] public double FaderValue { get; set; } = 0;
}