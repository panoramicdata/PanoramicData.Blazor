namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDPagerPage
    {
		private int CurrentPage { get; set; } = 1;
		private int PageCount { get; set; } = 5;
		private string Message { get; set; } = string.Empty;

		private void PageChangeHandler(int page)
		{
			Message = $"Current page changed to {page}";
			CurrentPage = page;
		}
	}
}
