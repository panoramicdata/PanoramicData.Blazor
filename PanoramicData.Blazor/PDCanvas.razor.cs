namespace PanoramicData.Blazor;

public partial class PDCanvas
{
	private static int _seq;

	[Parameter(CaptureUnmatchedValues = true)]
	public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

	[Parameter]
	public int Height { get; set; } = 300;

	[Parameter]
	public string Id { get; set; } = $"pd-canvas-{++_seq}";

	[Parameter]
	public int Width { get; set; } = 400;
}
