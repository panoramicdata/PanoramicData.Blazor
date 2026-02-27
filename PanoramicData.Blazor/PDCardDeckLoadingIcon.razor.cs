namespace PanoramicData.Blazor;

public partial class PDCardDeckLoadingIcon : IDisposable
{
	private DateTime _loadStart = DateTime.UtcNow;
	private int _currentLoadTime;
	private CancellationTokenSource? _cts;

	public bool IsActive { get; private set; }

	protected override async Task OnInitializedAsync()
	{
		_loadStart = DateTime.UtcNow;

		// Delay the start of the loading icon to allow the parent component to set up
		await Task.Delay(TimeSpan.FromSeconds(0.12));
		IsActive = true;
		_cts = new CancellationTokenSource();
		_ = UpdateElapsedTimeAsync(_cts.Token);
	}

	private async Task UpdateElapsedTimeAsync(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			_currentLoadTime = (int)(DateTime.UtcNow - _loadStart).TotalSeconds;
			await InvokeAsync(StateHasChanged);
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(1), token);

			}
			catch (TaskCanceledException)
			{
				break;
			}
		}
	}

	public void Dispose()
	{
		IsActive = false;
		_cts?.Cancel();
		_cts?.Dispose();
		GC.SuppressFinalize(this);
	}
}