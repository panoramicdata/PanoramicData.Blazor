namespace PanoramicData.Blazor;

/// <summary>
/// A modal dialog component with configurable size, buttons, and lifecycle callbacks.
/// </summary>
public partial class PDModal : IAsyncDisposable
{
	private static int _sequence;
	private TaskCompletionSource<string>? _userChoice;
	private IJSObjectReference? _module;
	private IJSObjectReference? _modalObj;
	private IJSObjectReference? _commonModule;
	private DotNetObjectReference<PDModal>? _dotNetReference;
	private bool _disposed;

	/// <summary>
	/// Gets or sets the JavaScript runtime used by modal interop.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Sets the modal button sizes in the footer
	/// </summary>
	[Parameter] public ButtonSizes ButtonSize { get; set; } = ButtonSizes.Medium;

	/// <summary>
	/// Sets additional CSS classes.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Sets additional CSS classes.
	/// </summary>
	[Parameter] public string HeaderCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Sets additional CSS classes.
	/// </summary>
	[Parameter] public string BodyCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Sets the content displayed in the modal dialog footer.
	/// </summary>
	[Parameter] public RenderFragment? Footer { get; set; }

	/// <summary>
	/// Sets the content displayed in the modal dialog header.
	/// </summary>
	[Parameter] public RenderFragment? Header { get; set; }

	/// <summary>
	/// An event callback that is invoked when the modal is hidden.
	/// </summary>
	[Parameter] public EventCallback Hidden { get; set; }

	/// <summary>
	/// Sets the title shown in the modal dialog header.
	/// </summary>
	[Parameter] public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Sets the content displayed in the modal dialog body.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// An event callback that is invoked when the modal is shown.
	/// </summary>
	[Parameter] public EventCallback Shown { get; set; }

	/// <summary>
	/// Sets the size of the modal dialog.
	/// </summary>
	[Parameter] public ModalSizes Size { get; set; } = ModalSizes.Medium;

	/// <summary>
	/// Sets the buttons displayed in the modal dialog footer.
	/// </summary>
	[Parameter]
	public List<ToolbarItem> Buttons { get; set; } =
	[
		new ToolbarButton { Key = ModalResults.YES, Text = "Yes", CssClass = "btn-primary", ShiftRight = true },
		new ToolbarButton { Key = ModalResults.NO, Text = "No" },
	];

	/// <summary>
	/// Event triggered whenever the user clicks on a button.
	/// </summary>
	[Parameter] public EventCallback<string> ButtonClick { get; set; }

	/// <summary>
	/// Close the modal when the user presses the escape key?
	/// </summary>
	[Parameter] public bool CloseOnEscape { get; set; } = true;

	/// <summary>
	/// Display the close button in the top right of the modal?
	/// </summary>
	[Parameter] public bool ShowClose { get; set; }

	/// <summary>
	/// Sets the title shown in the modal dialog header.
	/// </summary>
	[Parameter] public bool CenterVertically { get; set; }

	/// <summary>
	/// Should clicking on the background hide the modal?
	/// </summary>
	[Parameter] public bool HideOnBackgroundClick { get; set; }

	/// <summary>
	/// Gets the unique identifier of the modal.
	/// </summary>
	[Parameter] public string Id { get; set; } = $"pd-modal-{++_sequence}";

	/// <summary>
	/// Gets or sets whether the modal footer is rendered.
	/// </summary>
	[Parameter] public bool ShowFooter { get; set; } = true;

	/// <summary>
	/// Hides the Modal Dialog.
	/// </summary>
	public Task HideAsync() => HideAsync(default);

	/// <summary>
	/// Hides the Modal Dialog.
	/// </summary>
	public async Task HideAsync(CancellationToken cancellationToken)
	{
		if (_disposed || _modalObj is null)
		{
			return;
		}

		try
		{
			await _modalObj.InvokeVoidAsync("hide", cancellationToken).ConfigureAwait(true);
		}
		catch (ObjectDisposedException)
		{
			// The modal's JS reference was already disposed (e.g. Blazor tore down this
			// component's own child-component tree concurrently with a caller's explicit
			// teardown sequence). Nothing to hide - benign; ignore.
		}
		catch (JSDisconnectedException)
		{
			// The Blazor circuit has already disconnected. Nothing to hide - benign; ignore.
		}
	}

	/// <summary>
	/// Handles footer button clicks and either resolves awaiting callers or forwards the click event.
	/// </summary>
	/// <param name="args">The clicked button args.</param>
	public async Task OnButtonClick(KeyedEventArgs<MouseEventArgs> args)
	{
		// are we waiting for using response?
		if (_userChoice != null)
		{
			_userChoice.SetResult(args.Key);
		}
		else
		{
			// forward to calling app
			await ButtonClick.InvokeAsync(args.Key).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Initializes JavaScript interop for the modal after first render.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_dotNetReference = DotNetObjectReference.Create(this);
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl).ConfigureAwait(true);
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDModal.razor.js").ConfigureAwait(true);
				if (_module != null)
				{
					_modalObj = await _module.InvokeAsync<IJSObjectReference>("initialize", Id, new
					{
						backdrop = HideOnBackgroundClick ? (object)true : "static",
						focus = true,
						keyboard = CloseOnEscape
					}, _dotNetReference).ConfigureAwait(true);
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	/// <summary>
	/// Invoked from JavaScript when the modal has been shown.
	/// </summary>
	[JSInvokable]
	public async Task OnModalShown() => await Shown.InvokeAsync(null).ConfigureAwait(true);

	/// <summary>
	/// Invoked from JavaScript when the modal has been hidden.
	/// </summary>
	[JSInvokable]
	public async Task OnModalHidden() => await Hidden.InvokeAsync(null).ConfigureAwait(true);

	/// <summary>
	/// Displays the Modal Dialog.
	/// </summary>
	public Task ShowAsync() => ShowAsync(default);

	/// <summary>
	/// Displays the Modal Dialog.
	/// </summary>
	public async Task ShowAsync(CancellationToken cancellationToken)
	{
		if (_disposed || _modalObj is null)
		{
			return;
		}

		try
		{
			await _modalObj.InvokeVoidAsync("show", cancellationToken).ConfigureAwait(true);
		}
		catch (ObjectDisposedException)
		{
			// The modal's JS reference was already disposed. Nothing to show - benign; ignore.
		}
		catch (JSDisconnectedException)
		{
			// The Blazor circuit has already disconnected. Nothing to show - benign; ignore.
		}
	}

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync() => ShowAndWaitResultAsync(default);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	/// <param name="cancellationToken">Token used to cancel the async operation.</param>
	public async Task<string> ShowAndWaitResultAsync(CancellationToken cancellationToken)
	{
		// show dialog and await user choice.
		await ShowAsync(cancellationToken).ConfigureAwait(true);

		// focus first button with btn-primary class and key
		var btn = Buttons.Find(x =>
		{
			if (x is ToolbarButton btn)
			{
				return !string.IsNullOrWhiteSpace(btn.Key) && btn.CssClass.Contains("btn-primary", StringComparison.OrdinalIgnoreCase);
			}

			return false;
		});

		if (btn != null && _commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("focus", cancellationToken, $"pd-tbr-btn-{btn.Key}").ConfigureAwait(true);
		}

		_userChoice = new TaskCompletionSource<string>();
		var result = string.Empty;
		try
		{
			result = await _userChoice.Task.WaitAsync(cancellationToken).ConfigureAwait(true);
		}
		catch
		{
			// probably task cancelled exception - but capture any exception
		}

		await HideAsync(CancellationToken.None).ConfigureAwait(true);
		_userChoice = null;
		return result;
	}

	/// <summary>
	/// Releases JavaScript object references associated with this modal.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		GC.SuppressFinalize(this);

		try
		{
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
				_commonModule = null;
			}

			if (_module != null)
			{
				await _module.DisposeAsync().ConfigureAwait(true);
				_module = null;
			}

			if (_modalObj != null)
			{
				await _modalObj.DisposeAsync().ConfigureAwait(true);
				_modalObj = null;
			}
		}
		catch (ObjectDisposedException)
		{
			// Already disposed via a concurrent teardown path (e.g. Blazor's own child-component
			// disposal racing this call). Benign; ignore.
		}
		catch (JSDisconnectedException)
		{
			// The Blazor circuit has already disconnected. Benign; ignore.
		}

		_dotNetReference?.Dispose();
		_dotNetReference = null;
	}

	private string ModalCssClass
	{
		get
		{
			var sb = new StringBuilder();
			if (Size == ModalSizes.ExtraLarge)
			{
				sb.Append("modal-xl ");
			}
			else if (Size == ModalSizes.Large)
			{
				sb.Append("modal-lg ");
			}
			else if (Size == ModalSizes.Small)
			{
				sb.Append("modal-sm ");
			}

			if (CenterVertically)
			{
				sb.Append("modal-dialog-centered ");
			}

			return sb.ToString().TrimEnd();
		}
	}
}
