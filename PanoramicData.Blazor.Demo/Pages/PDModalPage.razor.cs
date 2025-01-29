namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDModalPage
	{
		private readonly CarDataProvider _dataProvider = new();

		private PDModal _modalPopup = null!;

		private Task OnClick(MouseEventArgs e)
			=> _modalPopup.ShowAsync();

		private Task CloseModal(MouseEventArgs e)
			=> _modalPopup.HideAsync();
	}
}
