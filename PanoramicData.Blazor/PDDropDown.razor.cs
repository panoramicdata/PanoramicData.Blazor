namespace PanoramicData.Blazor;

public partial class PDDropDown : IAsyncDisposable, IEnablable
{
	private bool _shown;
	private static int _sequence;
	private IJSObjectReference? _module;
	private IJSObjectReference? _dropdownObj;
	private DotNetObjectReference<PDDropDown>? _objRef;

	public enum CloseOptions
	{
		Inside,
		Outside,
		InsideOrOutside,
		Manual
	}

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// An event callback that is invoked when the dropdown is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> Click { get; set; }

	/// <summary>
	/// Gets or sets the child content of the dropdown.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets when the dropdown should close.
	/// </summary>
	[Parameter]
	public CloseOptions CloseOption { get; set; } = CloseOptions.Outside;

	/// <summary>
	/// Gets or sets the CSS class for the dropdown.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = "btn btn-primary dropdown-toggle";

	/// <summary>
	/// Gets or sets the direction the dropdown will open.
	/// </summary>
	[Parameter]
	public Directions DropdownDirection { get; set; } = Directions.Down;

	/// <summary>
	/// An event callback that is invoked when the dropdown is hidden.
	/// </summary>
	[Parameter]
	public EventCallback DropDownHidden { get; set; }

	/// <summary>
	/// An event callback that is invoked when the dropdown is shown.
	/// </summary>
	[Parameter]
	public EventCallback DropDownShown { get; set; }

	/// <summary>
	/// Gets or sets whether the dropdown is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the CSS class for the icon.
	/// </summary>
	[Parameter]
	public string IconCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the unique identifier for the dropdown.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-dropdown-{++_sequence}";

	/// <summary>
	/// An event callback that is invoked when a key is pressed.
	/// </summary>
	[Parameter]
	public EventCallback<int> KeyPress { get; set; }

	/// <summary>
	/// Gets or sets whether to prevent the default action of the event.
	/// </summary>
	[Parameter]
	public bool PreventDefault { get; set; }

	/// <summary>
	/// Gets or sets whether to show the caret.
	/// </summary>
	[Parameter]
	public bool ShowCaret { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the dropdown on mouse enter.
	/// </summary>
	[Parameter]
	public bool ShowOnMouseEnter { get; set; }

	/// <summary>
	/// Gets or sets the size of the dropdown button.
	/// </summary>
	[Parameter]
	public ButtonSizes Size { get; set; } = ButtonSizes.Medium;

	/// <summary>
	/// Gets or sets whether to stop the event from propagating further.
	/// </summary>
	[Parameter]
	public bool StopPropagation { get; set; }

	/// <summary>
	/// Gets or sets the text to be displayed on the dropdown button.
	/// </summary>
	[Parameter]
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the CSS class for the text.
	/// </summary>
	[Parameter]
	public string TextCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tooltip for the dropdown.
	/// </summary>
	[Parameter]
	public string ToolTip { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the dropdown is visible.
	/// </summary>
	[Parameter]
	public bool Visible { get; set; } = true;

	protected string ButtonSizeClass
		=> Size switch
		{
			ButtonSizes.Small => "btn-sm",
			ButtonSizes.Large => "btn-lg",
			_ => string.Empty
		};

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				_objRef?.Dispose();
				_objRef = null;

				if (_dropdownObj != null)
				{
					await _dropdownObj.DisposeAsync().ConfigureAwait(true);
					_dropdownObj = null;
				}

				if (_module != null)
				{
					await _module.DisposeAsync().ConfigureAwait(true);
					_module = null;
				}
			}
		}
		catch
		{
		}
	}

	public string DropdownId => $"{Id}-dropdown";

	public async Task HideAsync()
	{
		try
		{
			if (_dropdownObj != null)
			{
				await _dropdownObj.InvokeVoidAsync("hide").ConfigureAwait(true);
			}
		}
		catch
		{
			// Nothing to do
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_objRef = DotNetObjectReference.Create(this);
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDDropDown.razor.js").ConfigureAwait(true);
				if (_module != null)
				{
					_dropdownObj = await _module.InvokeAsync<IJSObjectReference>("initialize", Id, ToggleId, DropdownId, _objRef, new
					{
						autoClose = CloseOption switch
						{
							CloseOptions.Inside => (object)"inside",
							CloseOptions.InsideOrOutside => true,
							CloseOptions.Manual => false,
							_ => "outside"
						}
					}).ConfigureAwait(true);
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	[JSInvokable]
	public async Task OnDropDownShown()
	{
		try
		{
			_shown = true;
			await DropDownShown.InvokeAsync(null).ConfigureAwait(true);
			StateHasChanged();
		}
		catch
		{
			// Nothing to do
		}
	}

	[JSInvokable]
	public async Task OnDropDownHidden()
	{
		try
		{
			_shown = false;
			await DropDownHidden.InvokeAsync(null).ConfigureAwait(true);
			StateHasChanged();
		}
		catch
		{
			// Nothing to do
		}
	}

	[JSInvokable]
	public async Task OnKeyPressed(int keyCode)
	{
		try
		{
			await KeyPress.InvokeAsync(keyCode).ConfigureAwait(true);
		}
		catch
		{
			// Nothing to do
		}
	}

	public async Task ShowAsync()
	{
		try
		{
			if (_dropdownObj != null)
			{
				await _dropdownObj.InvokeVoidAsync("show").ConfigureAwait(true);
			}
		}
		catch
		{
			// Nothing to do
		}
	}

	private Task OnMouseEnter(MouseEventArgs args)
		=> IsEnabled && ShowOnMouseEnter ? ShowAsync() : Task.CompletedTask;

	[JSInvokable]
	public Task OnMouseLeave()
		=> IsEnabled && ShowOnMouseEnter ? HideAsync() : Task.CompletedTask;

	public async Task ToggleAsync()
	{
		try
		{
			if (_dropdownObj != null)
			{
				await _dropdownObj.InvokeVoidAsync("toggle").ConfigureAwait(true);
			}
		}
		catch
		{
			// Nothing to do
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

	public string ToggleId => $"{Id}-toggle";
}