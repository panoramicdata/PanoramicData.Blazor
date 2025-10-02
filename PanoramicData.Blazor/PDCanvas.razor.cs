namespace PanoramicData.Blazor;

public partial class PDCanvas
{
	private static int _seq;

	/// <summary>
	/// Gets or sets a collection of additional attributes that will be applied to the created element.
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)]
	public Dictionary<string, object> Attributes { get; set; } = [];

	/// <summary>
	/// Gets or sets the height of the canvas.
	/// </summary>
	[Parameter]
	public int Height { get; set; } = 300;

	/// <summary>
	/// Gets or sets the unique identifier for the canvas.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-canvas-{++_seq}";

	/// <summary>
	/// Gets or sets the width of the canvas.
	/// </summary>
	[Parameter]
	public int Width { get; set; } = 400;
}
