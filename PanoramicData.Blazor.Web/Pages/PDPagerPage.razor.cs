namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDPagerPage
    {
		private int _currentPage = 1;
		private int _pageCount = 5;
		private string _message = "";

		private void PageChangeHandler(int page)
		{
			_message = $"Current page changed to {page}";
			_currentPage = page;
		}
	}
}
