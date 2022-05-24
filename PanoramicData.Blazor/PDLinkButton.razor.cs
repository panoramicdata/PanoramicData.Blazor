namespace PanoramicData.Blazor;

public partial class PDLinkButton
{
	private static int _sequence;

	#region Inject
	[Inject] private IGlobalEventService GlobalEventService { get; set; } = null!;
	[Inject] public IJSRuntime? JSRuntime { get; set; }
	#endregion

	/// <summary>
	/// Gets or sets the button sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Extra attributes to apply to the button.
	/// </summary>
	[Parameter] public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

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
	[Parameter] public string Id { get; set; } = $"pdlb-{++_sequence}";

	/// <summary>
	/// Determines whether the button is enabled and can be clicked?
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Sets the short cut keys that will perform a click on this button.
	/// In format: 'ctrl-s', 'alt-ctrl-w' (case in-sensitive)
	/// </summary>
	[Parameter] public ShortcutKey ShortcutKey { get; set; } = new ShortcutKey();

	/// <summary>
	/// Sets where to display the linked URL, as the name for a browsing context (a tab, window, or <iframe>).
	/// The following keywords have special meanings for where to load the URL:
	/// _self: the current browsing context. (Default)
	/// _blank: usually a new tab, but users can configure browsers to open a new window instead.
	/// _parent: the parent browsing context of the current one. If no parent, behaves as _self.
	/// _top: the topmost browsing context (the "highest" context that’s an ancestor of the current one). If no ancestors, behaves as _self.
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
	/// Sets the destination URL.
	/// </summary>
	[Parameter] public string Url { get; set; } = "#";

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
			await ClickAsync().ConfigureAwait(true);
		}
	}

	public async Task ClickAsync()
	{
		if (JSRuntime != null)
		{
			await JSRuntime.InvokeVoidAsync("panoramicData.click", Id).ConfigureAwait(true);
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
