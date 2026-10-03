namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDGlobalListenerPage : IDisposable
{
	private readonly ShortcutKey _ctrlS = new() { Key = "s", CtrlKey = true };

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	[Inject] public IGlobalEventService? GlobalEventService { get; set; }

	private readonly EventHandler<KeyboardInfo> _keyDownHandler;
	private readonly EventHandler<KeyboardInfo> _keyUpHandler;

	public PDGlobalListenerPage()
	{
		// the event sender is not needed, only the keyboard details
		_keyDownHandler = (_, e) => AddKeyEvent("KeyDown", e);
		_keyUpHandler = (_, e) => AddKeyEvent("KeyUp", e);
	}

	protected override void OnInitialized()
	{
		if (GlobalEventService != null)
		{
			GlobalEventService.KeyDownEvent += _keyDownHandler;
			GlobalEventService.KeyUpEvent += _keyUpHandler;
			GlobalEventService.RegisterShortcutKey(_ctrlS);
		}
	}

	private void AddKeyEvent(string name, KeyboardInfo e) => EventManager?.Add(new Event(name, new EventArgument("Key", e.Key),
											 new EventArgument("AltKey", e.AltKey),
											 new EventArgument("ShiftKey", e.ShiftKey),
											 new EventArgument("CtrlKey", e.CtrlKey)));

	public void Dispose()
	{
		if (GlobalEventService != null)
		{
			GlobalEventService.KeyUpEvent -= _keyUpHandler;
			GlobalEventService.KeyDownEvent -= _keyDownHandler;
			GlobalEventService.UnregisterShortcutKey(_ctrlS);
		}

		GC.SuppressFinalize(this);
	}
}
