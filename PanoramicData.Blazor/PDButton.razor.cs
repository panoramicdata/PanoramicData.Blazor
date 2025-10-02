namespace PanoramicData.Blazor;

public partial class PDButton : IEnablable, IDisposable
{
	private bool _operationInProgress;

	#region Inject
	[Inject]
	private IGlobalEventService GlobalEventService { get; set; } = null!;
	#endregion

	/// <summary>
	/// Extra attributes to apply to the button.
	/// </summary>
	[Parameter] public Dictionary<string, object> Attributes { get; set; } = [];

	/// <summary>
	/// Custom content to display instead of the standard text and icon.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// CSS Class for button.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// CSS Class for icon to be displayed on button.
	/// </summary>
	[Parameter] public string IconCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Unique identifier for button.
	/// </summary>
	[Parameter] public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Determines whether the button is enabled and can be clicked?
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Sets a callback for when user clicks button.
	/// </summary>
	[Parameter] public EventCallback<MouseEventArgs> Click { get; set; }

	/// <summary>
	/// An event callback that is invoked when the mouse button is pressed down on the button.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> MouseDown { get; set; }

	/// <summary>
	/// An event callback that is invoked when the mouse pointer enters the button.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> MouseEnter { get; set; }

	/// <summary>
	/// Async function to be called when button is clicked.
	/// </summary>
	[Parameter]
	public Func<MouseEventArgs, Task>? Operation { get; set; }

	/// <summary>
	/// CSS Class for icon to be displayed on button when Operation is running.
	/// </summary>
	[Parameter] public string OperationIconCssClass { get; set; } = string.Empty;


	/// <summary>
	/// Gets or sets whether to prevent the default action of the event.
	/// </summary>
	[Parameter]
	public bool PreventDefault { get; set; }

	/// <summary>
	/// Gets or sets whether to stop the event from propagating further.
	/// </summary>
	[Parameter]
	public bool StopPropagation { get; set; }

	/// <summary>
	/// Gets or sets the button sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Sets the short cut keys that will perform a click on this button.
	/// In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive)
	/// </summary>
	[Parameter] public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey();

	/// <summary>
	/// Target where URL content should be opened.
	/// </summary>
	[Parameter] public string Target { get; set; } = "_self";

	/// <summary>
	/// Sets the text displayed on the button.
	/// </summary>
	[Parameter] public string Text { get; set; } = string.Empty;

	/// <summary>
	/// CSS Class for text to be displayed on button.
	/// </summary>
	[Parameter] public string TextCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Sets the text displayed on the buttons tooltip.
	/// </summary>
	[Parameter] public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Target URL. If set forces the button to be rendered as an Anchor element.
	/// </summary>
	[Parameter] public string Url { get; set; } = string.Empty;

	private bool ActualEnabledState => IsEnabled && (Operation is null || !_operationInProgress);

	private string ButtonSizeCssClass
	{
		get
		{
			return Size switch
			{
				ButtonSizes.Small => "btn-sm",
				ButtonSizes.Large => "btn-lg",
				_ => string.Empty,
			};
		}
	}

	private string GetIconCssClass()
	{
		return Operation != null && _operationInProgress && !string.IsNullOrWhiteSpace(OperationIconCssClass)
			? OperationIconCssClass
			: IconCssClass;
	}

	private async Task OnClickAsync(MouseEventArgs args)
	{
		if (Operation is null)
		{
			await Click.InvokeAsync(args);
		}
		else
		{
			_operationInProgress = true;
			await Operation.Invoke(args);
			_operationInProgress = false;
		}
	}

	protected override void OnInitialized()
	{
		GlobalEventService.KeyUpEvent += GlobalEventService_KeyUpEvent;
		if (ShortcutKey.HasValue)
		{
			GlobalEventService.RegisterShortcutKey(ShortcutKey);
		}
	}

	private async void GlobalEventService_KeyUpEvent(object? sender, KeyboardInfo e)
	{
		if (ShortcutKey.HasValue && ShortcutKey.IsMatch(e.Key, e.Code, e.AltKey, e.CtrlKey, e.ShiftKey))
		{
			await InvokeAsync(async () => await Click.InvokeAsync(new MouseEventArgs()).ConfigureAwait(true)).ConfigureAwait(true);
		}
	}

	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}

	public void Dispose()
	{
		if (ShortcutKey.HasValue)
		{
			GlobalEventService.UnregisterShortcutKey(ShortcutKey);
		}

		GlobalEventService.KeyUpEvent -= GlobalEventService_KeyUpEvent;
	}
}
