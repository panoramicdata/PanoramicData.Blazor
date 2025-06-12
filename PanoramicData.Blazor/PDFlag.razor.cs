namespace PanoramicData.Blazor;
public partial class PDFlag
{
	[EditorRequired]
	[Parameter]
	public required string CountryCode { get; set; } = string.Empty;

	[Parameter]
	public string Width { get; set; } = "2em";
}