namespace PanoramicData.Blazor.Models;

/// <summary>
/// Options for rendering and interacting with a timeline selection range.
/// </summary>
public class TimelineSelectionOptions
{
	/// <summary>
	/// Gets or sets the selection fill colour.
	/// </summary>
	public string BackgroundColour { get; set; } = "#04a0b563";
	/// <summary>
	/// Gets or sets the selection border colour.
	/// </summary>
	public string BorderColour { get; set; } = "#04a0b5";
	/// <summary>
	/// Gets or sets whether the selection end can be adjusted.
	/// </summary>
	public bool CanChangeEnd { get; set; } = true;
	/// <summary>
	/// Gets or sets whether the selection start can be adjusted.
	/// </summary>
	public bool CanChangeStart { get; set; } = true;
	/// <summary>
	/// Gets or sets whether selection interactions are enabled.
	/// </summary>
	public bool Enabled { get; set; } = true;
	/// <summary>
	/// Gets or sets the handle colour.
	/// </summary>
	public string HandleColour { get; set; } = "#7878789e";
	/// <summary>
	/// Gets or sets the handle width in pixels.
	/// </summary>
	public int HandleWidth { get; set; } = 4;

}
