namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that displays an animated loading indicator with an elapsed-time counter while card deck data is being fetched.
/// </summary>
public partial class PDCardDeckLoadingIcon : IDisposable
{
	/// <summary>
	/// The message shown when no other is given.
	/// </summary>
	public const string DefaultMessage = "Loading Data...";

	private readonly CancellationTokenSource _cts = new();
	private DateTime _loadStart = DateTime.UtcNow;
	private int _currentLoadTime;
	private bool _disposed;

	/// <summary>
	/// Gets a value indicating whether the loading icon is currently active and visible.
	/// </summary>
	public bool IsActive { get; private set; }

	/// <summary>
	/// Gets or sets the clock that supplies the current time and drives the start-up delay and the
	/// elapsed-time counter. Defaults to <see cref="TimeProvider.System"/>.
	/// </summary>
	[Parameter]
	public TimeProvider Clock { get; set; } = TimeProvider.System;

	/// <summary>
	/// Gets or sets the message shown under the loading cards. Defaults to <see cref="DefaultMessage"/>.
	/// </summary>
	[Parameter]
	public string Message { get; set; } = DefaultMessage;

	/// <summary>
	/// Gets or sets whether the message is followed by the number of seconds spent loading. Defaults to true.
	/// </summary>
	[Parameter]
	public bool ShowElapsedTime { get; set; } = true;

	private string MessageText => ShowElapsedTime
		? string.Create(CultureInfo.InvariantCulture, $"{Message} (current elapsed time {_currentLoadTime} seconds)")
		: Message;

	/// <inheritdoc />
	protected override async Task OnInitializedAsync()
	{
		_loadStart = Clock.GetUtcNow().UtcDateTime;
		var token = _cts.Token;

		// Delay the start of the loading icon to allow the parent component to set up. The icon is often
		// disposed during this delay (whenever data arrives quickly), and must then never start its timer.
		try
		{
			await Task.Delay(TimeSpan.FromSeconds(0.12), Clock, token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		if (token.IsCancellationRequested)
		{
			return;
		}

		IsActive = true;
		_ = UpdateElapsedTimeAsync(token);
	}

	private async Task UpdateElapsedTimeAsync(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			_currentLoadTime = (int)(Clock.GetUtcNow().UtcDateTime - _loadStart).TotalSeconds;
			await InvokeAsync(StateHasChanged);
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(1), Clock, token);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		IsActive = false;
		if (!_disposed)
		{
			_disposed = true;
			_cts.Cancel();
			_cts.Dispose();
		}

		GC.SuppressFinalize(this);
	}
}
