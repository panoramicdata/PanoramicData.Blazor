using PanoramicData.Blazor.Demo.Data;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDTreePage2
	{
		private readonly IDataProviderService<FileExplorerItem> _dataProviderOnDemand = new TestFileSystemDataProvider();

		private string GetNodeIconCssClass(FileExplorerItem _, int level)
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
			return $"fas fw fa-dice-{levelText}";
		}

		private int ReverseSort(FileExplorerItem a, FileExplorerItem b)
		{
			return a.Path.CompareTo(b.Path) * -1;
		}
	}
}
