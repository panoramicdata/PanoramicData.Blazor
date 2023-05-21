namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage2
{
	private readonly IDataProviderService<FileExplorerItem> _dataProviderOnDemand = new TestFileSystemDataProvider();

	private static string GetNodeIconCssClass(FileExplorerItem _, int level)
	{
		var levelText = (level) switch
		{
			1 => "one",
			2 => "two",
			3 => "three",
			4 => "four",
			5 => "five",
			_ => "six",
		};
		return $"fas fa-fw fa-dice-{levelText}";
	}

	private static int ReverseSort(FileExplorerItem a, FileExplorerItem b) => a.Path.CompareTo(b.Path) * -1;
}
