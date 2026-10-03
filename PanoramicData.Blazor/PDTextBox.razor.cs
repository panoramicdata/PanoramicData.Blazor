namespace PanoramicData.Blazor;

/// <summary>
/// Text input component with optional speech recognition, debouncing, and keyboard events.
/// </summary>
public partial class PDTextBox : IAsyncDisposable
{
	private static int _seq;
	private DotNetObjectReference<PDTextBox>? _objRef;
	private IJSObjectReference? _commonModule;

	/// <summary>
	/// Gets or sets JavaScript runtime used by this component.
	/// </summary>
	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the autocomplete attribute value.
	/// </summary>
	[Parameter]
	public string AutoComplete { get; set; } = string.Empty;

	/// <summary>
	/// Event raised when the text box loses focus.
	/// </summary>
	[Parameter]
	public EventCallback Blur { get; set; }

	/// <summary>
	/// Gets or sets the textbox sizes.
	/// </summary>
	[Parameter]
	public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Gets or sets CSS classes for the text box.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the input type.
	/// </summary>
	[Parameter]
	public PDInputType Type { get; set; } = PDInputType.Text;

	private string TypeString => Type == PDInputType.DateTimeLocal
		? "datetime-local"
		: Type.ToString().ToLowerInvariant();

	/// <summary>
	/// Gets whether keypress events are raised.
	/// </summary>
	[Parameter]
	public bool KeypressEvent { get; set; }

	/// <summary>
	/// Gets or sets the speech recognition language. Leave empty for browser default.
	/// </summary>
	[Parameter]
	public string SpeechLang { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tooltip for the toolbar item.
	/// </summary>
	[Parameter]
	public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the content is read only.
	/// </summary>
	[Parameter]
	public bool IsReadOnly { get; set; }

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	[Parameter]
	public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Sets the width of the containing div element.
	/// </summary>
	[Parameter]
	public string Width { get; set; } = "Auto";

	/// <summary>
	/// Gets or sets placeholder text for the text box.
	/// </summary>
	[Parameter]
	public string Placeholder { get; set; } = string.Empty;

	/// <summary>
	/// Sets the initial text value.
	/// </summary>
	[Parameter]
	public string Value { get; set; } = string.Empty;

	/// <summary>
	/// Event raised whenever the text value changes.
	/// </summary>
	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	/// <summary>
	/// Gets or sets the event that triggers binding, e.g. oninput or onchange.
	/// </summary>
	[Parameter]
	public string BindEvent { get; set; } = "oninput";

	/// <summary>
	/// Event raised whenever a key is pressed.
	/// </summary>
	[Parameter]
	public EventCallback<KeyboardEventArgs> Keypress { get; set; }

	/// <summary>
	/// Gets or sets whether the clear button is displayed.
	/// </summary>
	[Parameter]
	public bool ShowClearButton { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the user may use speech to populate the textbox.
	/// </summary>
	[Parameter]
	public bool ShowSpeechButton { get; set; }

	/// <summary>
	/// Sets the debounce wait period in milliseconds.
	/// </summary>
	[Parameter]
	public int DebounceWait { get; set; }

	/// <summary>
	/// Event raised when the user clicks on the clear button.
	/// </summary>
	[Parameter]
	public EventCallback Cleared { get; set; }

	/// <summary>
	/// Gets the unique identifier for this text box instance.
	/// </summary>
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

	/// <summary>
	/// Initializes JavaScript helpers and optional debounced input/speech support.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_objRef = DotNetObjectReference.Create(this);
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
				if (_commonModule != null && DebounceWait > 0)
				{
					await _commonModule.InvokeVoidAsync("debounceInput", Id, DebounceWait, _objRef).ConfigureAwait(true);
				}

				if (ShowSpeechButton)
				{
					await InitializeSpeechAsync().ConfigureAwait(true);
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	private async Task OnBlur() => await Blur.InvokeAsync().ConfigureAwait(true);

	private async Task OnChange(ChangeEventArgs args)
	{
		if (DebounceWait <= 0)
		{
			Value = args.Value?.ToString() ?? string.Empty;
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
		}
	}

	private async Task OnClear()
	{
		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("setValue", Id, string.Empty).ConfigureAwait(true);
		}

		Value = string.Empty;
		await ValueChanged.InvokeAsync(string.Empty).ConfigureAwait(true);
		await Cleared.InvokeAsync(null).ConfigureAwait(true);
	}

	/// <summary>
	/// Receives debounced input updates from JavaScript.
	/// </summary>
	/// <param name="value">Updated text value.</param>
	[JSInvokable]
	public async Task OnDebouncedInput(string value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(value).ConfigureAwait(true);
	}

	private async Task OnKeypress(KeyboardEventArgs args)
	{
		await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);

		await Keypress.InvokeAsync(args).ConfigureAwait(true);
	}

	/// <summary>
	/// Disposes JavaScript resources and event references used by this component.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}

			await DisposeSpeechAsync().ConfigureAwait(true);

			_objRef?.Dispose();
		}
		catch
		{
			// BC-40 - the circuit may already be gone, in which case there is no JavaScript side left to tear down
		}
	}
}