namespace PanoramicData.Blazor;

public partial class PDTextArea : IAsyncDisposable, IEnablable
{
	private static int _seq;
	private bool _cancelDebounce;
	private IJSObjectReference? _module;
	private IJSObjectReference? _commonModule;
	private TextAreaSelection _selection = new();
	private DotNetObjectReference<PDTextArea>? _objRef;

	/// <summary>
	/// Injected javascript interop object.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter] public EventCallback Blur { get; set; }

	/// <summary>
	/// Gets or sets CSS classes for the text box.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = "";

	/// <summary>
	/// Gets or sets the tooltip for the toolbar item.
	/// </summary>
	[Parameter] public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	[Parameter] public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is enabled.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the content is read only.
	/// </summary>
	[Parameter] public bool IsReadOnly { get; set; }

	/// <summary>
	/// Sets the width of the containing div element.
	/// </summary>
	[Parameter] public string Width { get; set; } = "Auto";

	/// <summary>
	/// Sets the maximum length of the input.
	/// </summary>
	[Parameter] public int MaxLength { get; set; } = -1;

	/// <summary>
	/// Gets or sets placeholder text for the text box.
	/// </summary>
	[Parameter] public string Placeholder { get; set; } = string.Empty;

	/// <summary>
	/// Event raised whenever the text value changes.
	/// </summary>
	[Parameter] public EventCallback<TextAreaSelection> SelectionChanged { get; set; }

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
	/// Sets the number of rows displayed.
	/// </summary>
	[Parameter] public int Rows { get; set; } = 5;

	/// <summary>
	/// Gets or sets whether the clear button is displayed.
	/// </summary>
	[Parameter] public bool ShowClearButton { get; set; } = true;

	/// <summary>
	/// Sets the de-bounce wait period in milliseconds.
	/// </summary>
	[Parameter] public int DebounceWait { get; set; }

	/// <summary>
	/// Event raised when the user clicks on the clear button.
	/// </summary>
	[Parameter] public EventCallback Cleared { get; set; }

	public string Id { get; set; } = $"pd-textarea-{++_seq}";

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}

			if (_module != null)
			{
				await _module.InvokeVoidAsync("termTextArea", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}

			_objRef?.Dispose();
		}
		catch
		{
		}
	}

	public TextAreaSelection GetSelection() => _selection;

	private Task OnAfter()
	{
		if (DebounceWait <= 0)
		{
			// invoke the event
			ValueChanged.InvokeAsync(Value);
		}
		return Task.CompletedTask;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_objRef = DotNetObjectReference.Create(this);
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js");
				if (_commonModule != null && DebounceWait > 0)
				{
					await _commonModule.InvokeVoidAsync("debounceInput", Id, DebounceWait, _objRef).ConfigureAwait(true);
				}

				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDTextArea.razor.js").ConfigureAwait(true);
				if (_module != null)
				{
					await _module.InvokeVoidAsync("initTextArea", Id, _objRef).ConfigureAwait(true);
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	private async Task OnBlur(FocusEventArgs args)
	{
		if (DebounceWait > 0 && _commonModule != null)
		{
			// ignore next debounced blur
			_cancelDebounce = true;

			// TODO: if running within a PDForm then need to block from
			// switching to another Item until completes
			var val = await _commonModule.InvokeAsync<string>("getValue", Id);
			await ValueChanged.InvokeAsync(val).ConfigureAwait(true);

		}

		await Blur.InvokeAsync().ConfigureAwait(true);
	}

	[JSInvokable]
	public async Task OnDebouncedInput(string value)
	{
		if (DebounceWait > 0 && !_cancelDebounce)
		{
			Value = value;
			await ValueChanged.InvokeAsync(value).ConfigureAwait(true);
		}

		_cancelDebounce = false;
	}


	private async Task OnInput(ChangeEventArgs args)
	{
		if (DebounceWait <= 0)
		{
			Value = args.Value?.ToString() ?? string.Empty;
			await ValueChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty).ConfigureAwait(true);
		}
	}


	private async Task OnKeypress(KeyboardEventArgs args)
	{
		if (DebounceWait <= 0)
		{
			await ValueChanged.InvokeAsync(Value).ConfigureAwait(true);
			await Keypress.InvokeAsync(args).ConfigureAwait(true);
		}
	}

	[JSInvokable]
	public async Task OnSelectionChanged(int start, int end, string value)
	{
		if (start != _selection.Start || end != _selection.End)
		{
			_selection = new TextAreaSelection
			{
				Start = start,
				End = end,
				Value = value
			};
			await SelectionChanged.InvokeAsync(_selection);
		}
	}

	public async Task SetSelectionAsync(int start, int end)
	{
		if (_module != null)
		{
			await _module.InvokeVoidAsync("setSelection", Id, start, end);
		}
	}

	public async Task SetValueAsync(string value)
	{
		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("setValue", Id, value);
		}
	}

	public async Task ScrollToEndAsync()
	{
		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("scrollToEnd", Id);
		}
	}

	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}
}
