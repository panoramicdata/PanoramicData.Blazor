namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDSplitterPage
{
	private PDSplitter _mainSplitter = null!;
	private double[] _lastSizes = new double[0];
	private bool _isCollapsed;

	private async Task OnTogglePanel2()
	{
		if (_isCollapsed)
		{
			// restore
			await _mainSplitter.SetSizesAsync(_lastSizes).ConfigureAwait(true);
			_isCollapsed = false;
		}
		else
		{
			_lastSizes = await _mainSplitter.GetSizesAsync().ConfigureAwait(true);
			await _mainSplitter.SetSizesAsync(new double[] { _lastSizes[0] + _lastSizes[1], 0, _lastSizes[2] }).ConfigureAwait(true);
			_isCollapsed = true;
		}
	}
}
