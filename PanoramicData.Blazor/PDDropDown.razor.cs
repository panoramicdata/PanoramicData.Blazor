namespace PanoramicData.Blazor;

public partial class PDDropDown : IAsyncDisposable
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

	[Parameter]
	public EventCallback<MouseEventArgs> Click { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	[Parameter]
	public CloseOptions CloseOption { get; set; } = CloseOptions.Outside;

	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	[Parameter]
	public Directions DropdownDirection { get; set; } = Directions.Down;

	[Parameter]
	public EventCallback DropDownHidden { get; set; }

	[Parameter]
	public EventCallback DropDownShown { get; set; }

	[Parameter]
	public bool IsEnabled { get; set; } = true;

	[Parameter]
	public string IconCssClass { get; set; } = string.Empty;

	[Parameter]
	public string Id { get; set; } = $"pd-dropdown-{++_sequence}";

	[Parameter]
	public EventCallback<int> KeyPress { get; set; }

	[Parameter]
	public bool PreventDefault { get; set; }

	[Parameter]
	public bool ShowCaret { get; set; } = true;

	[Parameter]
	public bool ShowOnMouseEnter { get; set; }

	[Parameter]
	public ButtonSizes Size { get; set; } = ButtonSizes.Medium;

	[Parameter]
	public bool StopPropagation { get; set; }

	[Parameter]
	public string Text { get; set; } = string.Empty;

	[Parameter]
	public string TextCssClass { get; set; } = string.Empty;

	[Parameter]
	public string ToolTip { get; set; } = string.Empty;

	[Parameter]
	public bool Visible { get; set; } = true;

	private static Dictionary<string, object> Attributes
	{
		get
		{
			return new Dictionary<string, object>
			{
				{ "data-bs-toggle", "dropdown" },
				{ "data-bs-offset", "0,-3" } // required to leave no gap between toggle button and dropdown
			};
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				if (_objRef != null)
				{
					_objRef.Dispose();
					_objRef = null;
				}

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
		if (_dropdownObj != null)
		{
			await _dropdownObj.InvokeVoidAsync("hide").ConfigureAwait(true);
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
		_shown = true;
		await DropDownShown.InvokeAsync(null).ConfigureAwait(true);
		StateHasChanged();
	}

	[JSInvokable]
	public async Task OnDropDownHidden()
	{
		_shown = false;
		await DropDownHidden.InvokeAsync(null).ConfigureAwait(true);
		StateHasChanged();
	}

	[JSInvokable]
	public async Task OnKeyPressed(int keyCode) => await KeyPress.InvokeAsync(keyCode).ConfigureAwait(true);

	public async Task ShowAsync()
	{
		if (_dropdownObj != null)
		{
			await _dropdownObj.InvokeVoidAsync("show").ConfigureAwait(true);
		}
	}

	private Task OnMouseEnter(MouseEventArgs args)
		=> IsEnabled && ShowOnMouseEnter ? ShowAsync() : Task.CompletedTask;

	[JSInvokable]
	public Task OnMouseLeave()
		=> IsEnabled && ShowOnMouseEnter ? HideAsync() : Task.CompletedTask;

	public async Task ToggleAsync()
	{
		if (_dropdownObj != null)
		{
			await _dropdownObj.InvokeVoidAsync("toggle").ConfigureAwait(true);
		}
	}

	public string ToggleId => $"{Id}-toggle";
}
