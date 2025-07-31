using PanoramicData.Blazor.Services;
namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage4
{
	private PDTree<TreeItem>? Tree { get; set; }
	private readonly IDataProviderService<TreeItem> _treeDataProvider;

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	public PDTreePage4()
	{
		_treeDataProvider = new DelegatedDataProviderService<TreeItem>
		{
			GetDataAsync = (DataRequest, cancellationToken) =>
			{
				var items = new List<TreeItem>
				{
					new ()
					{
						Id = 1,
						Name = "Search Engines",
						IconCssClass = "fas fa-fw fa-search me-1",
						IsGroup = true
					},
					new ()
					{
						Id = 101,
						Name = "Bing",
						ParentId = 1,
						IconCssClass="fas fa-fw fa-external-link-alt me-1",

					},
					new ()
					{
						Id = 102,
						Name = "DuckDuckGo",
						ParentId = 1,
						IconCssClass="fas fa-fw fa-external-link-alt me-1"
					},
					new ()
					{
						Id = 103,
						Name = "Google",
						ParentId = 1,
						IconCssClass="fas fa-fw fa-external-link-alt me-1"
					},
					new ()
					{
						Id = 104,
						Name = "Presearch",
						ParentId = 1,
						IconCssClass="fas fa-fw fa-external-link-alt me-1"
					},
					new ()
					{
						Id = 2,
						Name = "Weather",
						IconCssClass = "fas fa-fw fa-cloud-sun-rain me-1",
						IsGroup = true
					},
					new ()
					{
						Id = 201,
						Name = "BBC Weather",
						ParentId = 2,
						IconCssClass="fas fa-fw fa-external-link-square-alt me-1"
					},
					new ()
					{
						Id = 202,
						Name = "MetOffice",
						ParentId = 2,
						IconCssClass="fas fa-fw fa-external-link-square-alt me-1"
					},
					new ()
					{
						Id = 203,
						Name = "Weather.com",
						ParentId = 2,
						IconCssClass="fas fa-fw fa-external-link-square-alt me-1"
					}
				};
				return Task.FromResult(new DataResponse<TreeItem>(items, items.Count));
			}
		};
	}

	private void OnReady() => Tree?.ExpandAll();
}
