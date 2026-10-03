namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDSplitterPage
{
	protected PDSplitter MainSplitter { get; set; } = null!;
	private double[] _lastSizes = [];
	private bool _isCollapsed;

	private async Task OnTogglePanel2()
	{
		if (_isCollapsed)
		{
			// restore
			await MainSplitter.SetSizesAsync(_lastSizes).ConfigureAwait(true);
			_isCollapsed = false;
		}
		else
		{
			_lastSizes = await MainSplitter.GetSizesAsync().ConfigureAwait(true);
			await MainSplitter.SetSizesAsync([_lastSizes[0] + _lastSizes[1], 0, _lastSizes[2]]).ConfigureAwait(true);
			_isCollapsed = true;
		}
	}
}
