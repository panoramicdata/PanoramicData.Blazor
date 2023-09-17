namespace PanoramicData.Blazor;

public partial class PDButton : IDisposable
{
	#region Inject
	[Inject]
	private IGlobalEventService GlobalEventService { get; set; } = null!;
	#endregion

	/// <summary>
	/// Extra attributes to apply to the button.
	/// </summary>
	[Parameter] public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

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

	[Parameter]
	public EventCallback<MouseEventArgs> MouseDown { get; set; }

	[Parameter]
	public bool PreventDefault { get; set; }

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

	public void Dispose()
	{
		if (ShortcutKey.HasValue)
		{
			GlobalEventService.UnregisterShortcutKey(ShortcutKey);
		}

		GlobalEventService.KeyUpEvent -= GlobalEventService_KeyUpEvent;
	}
}
