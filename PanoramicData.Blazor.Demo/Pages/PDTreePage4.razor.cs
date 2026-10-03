using PanoramicData.Blazor.Services;
namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage4
{
	protected PDTree<TreeItem>? Tree { get; set; }
	private readonly IDataProviderService<TreeItem> _treeDataProvider;

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	public PDTreePage4()
	{
		_treeDataProvider = new DelegatedDataProviderService<TreeItem>
		{
			GetDataAsync = (_, _) =>
			{
				var items = CreateItems();
				return Task.FromResult(new DataResponse<TreeItem>(items, items.Count));
			}
		};
	}

	private static List<TreeItem> CreateItems() =>
	[
		Group(1, "Search Engines", "fas fa-fw fa-search me-1"),
		Link(101, "Bing", 1, "fas fa-fw fa-external-link-alt me-1"),
		Link(102, "DuckDuckGo", 1, "fas fa-fw fa-external-link-alt me-1"),
		Link(103, "Google", 1, "fas fa-fw fa-external-link-alt me-1"),
		Link(104, "Presearch", 1, "fas fa-fw fa-external-link-alt me-1"),
		Group(2, "Weather", "fas fa-fw fa-cloud-sun-rain me-1"),
		Link(201, "BBC Weather", 2, "fas fa-fw fa-external-link-square-alt me-1"),
		Link(202, "MetOffice", 2, "fas fa-fw fa-external-link-square-alt me-1"),
		Link(203, "Weather.com", 2, "fas fa-fw fa-external-link-square-alt me-1")
	];

	private static TreeItem Group(int id, string name, string iconCssClass)
		=> new() { Id = id, Name = name, IconCssClass = iconCssClass, IsGroup = true };

	private static TreeItem Link(int id, string name, int parentId, string iconCssClass)
		=> new() { Id = id, Name = name, ParentId = parentId, IconCssClass = iconCssClass };

	private void OnReady() => Tree?.ExpandAll();
}
