namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that displays an animated loading indicator with an elapsed-time counter while card deck data is being fetched.
/// </summary>
public partial class PDCardDeckLoadingIcon : IDisposable
{
	private DateTime _loadStart = DateTime.UtcNow;
	private int _currentLoadTime;
	private CancellationTokenSource? _cts;

	/// <summary>
	/// Gets a value indicating whether the loading icon is currently active and visible.
	/// </summary>
	public bool IsActive { get; private set; }

	/// <inheritdoc />
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

	/// <inheritdoc />
	public void Dispose()
	{
		IsActive = false;
		_cts?.Cancel();
		_cts?.Dispose();
		GC.SuppressFinalize(this);
	}
}