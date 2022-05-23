namespace PanoramicData.Blazor;

public partial class PDTextBox : IDisposable
{
	private static int _seq;
	private DotNetObjectReference<PDTextBox>? _objRef;

	/// <summary>
	/// Injected javascript interop object.
	/// </summary>
	[Inject] public IJSRuntime? JSRuntime { get; set; }

	/// <summary>
	/// Gets or sets the autocomplete attribute value.
	/// </summary>
	[Parameter] public string AutoComplete { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the textbox sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Gets or sets CSS classes for the text box.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = "";

	/// <summary>
	/// Gets whether Keypress events are raised.
	/// </summary>
	[Parameter] public bool KeypressEvent { get; set; }

	/// <summary>
	/// Gets or sets the tooltip for the toolbar item.
	/// </summary>
	[Parameter] public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the content is read only.
	/// </summary>
	[Parameter] public bool IsReadOnly { get; set; }

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	[Parameter] public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is enabled.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Sets the width of the containing div element.
	/// </summary>
	[Parameter] public string Width { get; set; } = "Auto";

	/// <summary>
	/// Gets or sets placeholder text for the text box.
	/// </summary>
	[Parameter] public string Placeholder { get; set; } = string.Empty;

	/// <summary>
	/// Sets the initial text value.
	/// </summary>
	[Parameter] public string Value { get; set; } = string.Empty;

	/// <summary>
	/// Event raised whenever the text value changes.
	/// </summary>
	[Parameter] public EventCallback<string> ValueChanged { get; set; }

	/// <summary>
	/// Event raised whenever a key is pressed.
	/// </summary>
	[Parameter] public EventCallback<KeyboardEventArgs> Keypress { get; set; }

	/// <summary>
	/// Gets or sets whether the clear button is displayed.
	/// </summary>
	[Parameter] public bool ShowClearButton { get; set; } = true;

	/// <summary>
	/// Sets the debounce wait period in milliseconds.
	/// </summary>
	[Parameter] public int DebounceWait { get; set; }

	/// <summary>
	/// Event raised when the user clicks on the clear button.
	/// </summary>
	[Parameter] public EventCallback Cleared { get; set; }

	public string Id { get; set; } = $"pd-textbox-{++_seq}";

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

	private string TextSizeCssClass
	{
		get
		{
			return Size switch
			{
				ButtonSizes.Small => "form-control-sm",
				ButtonSizes.Large => "form-control-lg",
				_ => string.Empty,
			};
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && DebounceWait > 0)
		{
			_objRef = DotNetObjectReference.Create(this);
			await JSRuntime.InvokeVoidAsync("panoramicData.debounceInput", Id, DebounceWait, _objRef).ConfigureAwait(true);
		}
	}

	//private async Task OnInput(ChangeEventArgs args)
	//{
	//	if (DebounceWait <= 0)
	//	{
	//		Value = args.Value.ToString();
	//		await ValueChanged.InvokeAsync(args.Value.ToString()).ConfigureAwait(true);
	//	}
	//}

	private async Task OnChange(ChangeEventArgs args)
	{
		Value = args.Value.ToString();
		await ValueChanged.InvokeAsync(args.Value.ToString()).ConfigureAwait(true);
	}

	[JSInvokable]
	public async Task OnDebouncedInput(string value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(value).ConfigureAwait(true);
	}

	private async Task OnKeypress(KeyboardEventArgs args)
	{
		await Keypress.InvokeAsync(args).ConfigureAwait(true);
	}

	private async Task OnClear(MouseEventArgs _)
	{
		await JSRuntime.InvokeVoidAsync("panoramicData.setValue", Id, string.Empty).ConfigureAwait(true);
		Value = string.Empty;
		await ValueChanged.InvokeAsync(string.Empty).ConfigureAwait(true);
		await Cleared.InvokeAsync(null).ConfigureAwait(true);
	}

	public void Dispose()
	{
		_objRef?.Dispose();
	}
}
