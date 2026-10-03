namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDVoiceListenerPage : IDisposable
{
	private readonly List<ListenerInput> _transcript = [];

	[Inject] public IListenerService ListenerService { get; set; } = null!;

	protected PDVoiceListener? ListenerComponent { get; set; }

	private IReadOnlyList<ListenerInput> Transcript => _transcript;

	private ListenerState CurrentState { get; set; } = ListenerState.Idle;

	private ListenerMode SelectedMode
	{
		get;
		set
		{
			if (field == value)
			{
				return;
			}

			field = value;
			_transcript.Clear();
			CurrentState = value == ListenerMode.KeywordActivation ? ListenerState.ActiveAwaitingKeyword : ListenerState.Idle;
		}
	} = ListenerMode.ManualActivation;

	private string Keyword { get; set; } = "computer";

	private static TimeSpan KeywordSilenceTimeout => TimeSpan.FromSeconds(3);

	private readonly EventHandler<ListenerInput> _inputReceivedHandler;

	public PDVoiceListenerPage()
	{
		// the event sender is not needed, only the input received
		_inputReceivedHandler = (_, e) => OnInputReceived(e);
	}

	protected override void OnInitialized()
	{
		ListenerService.InputReceived += _inputReceivedHandler;
		ListenerService.StatusChanged += ListenerService_StatusChanged;
	}

	private async Task StartManualAsync()
	{
		if (ListenerComponent != null)
		{
			await ListenerComponent.StartAsync().ConfigureAwait(true);
		}
	}

	private async Task StopManualAsync()
	{
		if (ListenerComponent != null)
		{
			await ListenerComponent.StopAsync().ConfigureAwait(true);
		}
	}

	private void OnInputReceived(ListenerInput e)
	{
		_transcript.Insert(0, e);
		if (_transcript.Count > 50)
		{
			_transcript.RemoveAt(_transcript.Count - 1);
		}

		_ = InvokeAsync(StateHasChanged);
	}

	private void ListenerService_StatusChanged(object? sender, ListenerStatusChangedEventArgs e)
	{
		CurrentState = e.State;
		_ = InvokeAsync(StateHasChanged);
	}

	public void Dispose()
	{
		ListenerService.InputReceived -= _inputReceivedHandler;
		ListenerService.StatusChanged -= ListenerService_StatusChanged;
		GC.SuppressFinalize(this);
	}
}
