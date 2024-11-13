namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTreePage4
{
	private PDTree<TreeItem>? _tree { get; set; }
	private readonly IDataProviderService<TreeItem> _treeDataProvider;

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	public PDTreePage4()
	{
		_treeDataProvider = new DelegatedDataProvider<TreeItem>
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
						Target = "_blank",
						Url = "https://www.bing.com/"
					},
					new ()
					{
						Id = 102,
						Name = "DuckDuckGo",
						ParentId = 1,
						IconCssClass="fas fa-fw fa-external-link-alt me-1",
						Target = "_blank",
						Url = "https://duckduckgo.com/"
					},
					new ()
					{
						Id = 103,
						Name = "Google",
						ParentId = 1,
						IconCssClass="fas fa-fw fa-external-link-alt me-1",
						Target = "_blank",
						Url = "https://google.co.uk"
					},
					new ()
					{
						Id = 104,
						Name = "Presearch",
						ParentId = 1,
						IconCssClass="fas fa-fw fa-external-link-alt me-1",
						Target = "_blank",
						Url = "//https://presearch.com/"
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
						IconCssClass="fas fa-fw fa-external-link-square-alt me-1",
						Url = "https://www.bbc.co.uk/weather"
					},
					new ()
					{
						Id = 202,
						Name = "MetOffice",
						ParentId = 2,
						IconCssClass="fas fa-fw fa-external-link-square-alt me-1",
						Url = "https://www.metoffice.gov.uk/"
					},
					new ()
					{
						Id = 203,
						Name = "Weather.com",
						ParentId = 2,
						IconCssClass="fas fa-fw fa-external-link-square-alt me-1",
						Url = "https://weather.com/"
					}
				};
				return Task.FromResult(new DataResponse<TreeItem>(items, items.Count));
			}
		};
	}

	private void OnReady() => _tree?.ExpandAll();
}
