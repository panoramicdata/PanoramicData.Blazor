using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDTablePage
	{
		public TestDataProvider DataProvider { get; }  = new TestDataProvider();
	}
}
