using System.Collections.Generic;
using Newtonsoft.Json;
using PanoramicData.Blazor.Web.Data;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDTablePage
	{
		// columns config enables config to be defined per user or customer etc.
		private List<PDColumnConfig>? _columnsConfig = new List<PDColumnConfig>
			{
				new PDColumnConfig { Id = "Col1" },
				new PDColumnConfig { Id = "Col2", Title = "Date Started" },
				new PDColumnConfig { Id = "Col3" },
				new PDColumnConfig { Id = "Col4" }
			};

		private string ColumnsConfigJson
		{
			get
			{
				return JsonConvert.SerializeObject(_columnsConfig, Formatting.Indented);
			}
			set
			{
				_columnsConfig = JsonConvert.DeserializeObject<List<PDColumnConfig>>(value);
			}
		}

		// dummy data provider
		public TestDataProvider DataProvider { get; }  = new TestDataProvider();
	}
}
