namespace PanoramicData.Blazor.Models;

public class PageState
{
	public uint Page { get; set; } = 1;
	public uint PageCount { get; set; }
	public uint PageSize { get; set; } = 10;
}