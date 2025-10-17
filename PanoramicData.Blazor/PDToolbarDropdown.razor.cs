using static PanoramicData.Blazor.PDDropDown;

namespace PanoramicData.Blazor;

public partial class PDToolbarDropdown : IDisposable, IEnablable
{
	#region Inject
	[Inject] IGlobalEventService GlobalEventService { get; set; } = null!;
	#endregion

	/// <summary>
	/// Child HTML content.
	/// </summary>
	[Parameter] public RenderFragment ChildContent { get; set; } = null!;

	/// <summary>
	/// Event raised whenever user clicks on the button.
	/// </summary>
	[Parameter] public EventCallback<string> Click { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier.
	/// </summary>
	[Parameter] public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the button sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Gets or sets the text displayed on the button.
	/// </summary>
	[Parameter] public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Determines when the dropdown should close.
	/// </summary>
	[Parameter]
	public CloseOptions CloseOption { get; set; } = CloseOptions.InsideOrOutside;

	/// <summary>
	/// Gets or sets CSS classes for the button.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = "btn-secondary";

	/// <summary>
	/// Gets or sets CSS classes for the toolbar item.
	/// </summary>
	[Parameter] public string ItemCssClass { get; set; } = "";

	/// <summary>
	/// Gets or sets CSS classes for an optional icon.
	/// </summary>
	[Parameter] public string IconCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	[Parameter] public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the toolbar item is enabled.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the menu items to be displayed in the context menu.
	/// </summary>
	[Parameter] public List<MenuItem> Items { get; set; } = [];

	/// <summary>
	/// Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item.
	/// </summary>
	[Parameter] public bool ShiftRight { get; set; }

	/// <summary>
	/// Gets or sets whether the dropdown is shown on mouse enter.
	/// </summary>
	[Parameter]
	public bool ShowOnMouseEnter { get; set; }

	/// <summary>
	/// Gets or sets CSS classes for the text.
	/// </summary>
	[Parameter] public string TextCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tooltip for the toolbar item.
	/// </summary>
	[Parameter] public string ToolTip { get; set; } = string.Empty;

	public void AddMenuItem(PDMenuItem item)
	{
		Items.Add(new MenuItem
		{
			Content = item.Content,
			IconCssClass = item.IconCssClass,
			IsDisabled = item.IsDisabled,
			IsSeparator = item.IsSeparator,
			IsVisible = item.IsVisible,
			Key = item.Key,
			ShortcutKey = item.ShortcutKey,
			Text = item.Text
		});
		StateHasChanged();
	}

	private string ButtonSizeCssClass
	{
		get
		{
			return Size switch
			{
				ButtonSizes.Small => "btn-sm",
				ButtonSizes.Large => "btn-lg",
				_ => string.Empty
			};
		}
	}

	protected override void OnInitialized()
	{
		GlobalEventService.KeyUpEvent += GlobalEventService_KeyUpEvent;
		foreach (var item in Items)
		{
			if (item.ShortcutKey.HasValue)
			{
				GlobalEventService.RegisterShortcutKey(item.ShortcutKey);
			}
		}
	}

	private async void GlobalEventService_KeyUpEvent(object? sender, KeyboardInfo e)
	{
		foreach (var item in Items)
		{
			if (item.ShortcutKey.IsMatch(e.Key, e.Code, e.AltKey, e.CtrlKey, e.ShiftKey))
			{
				await InvokeAsync(async () => await OnClick(item.GetKeyOrText()).ConfigureAwait(true)).ConfigureAwait(true);
				break;
			}
		}
	}

	public void Dispose()
	{
		foreach (var item in Items)
		{
			if (item.ShortcutKey.HasValue)
			{
				GlobalEventService.UnregisterShortcutKey(item.ShortcutKey);
			}
		}

		GlobalEventService.KeyUpEvent -= GlobalEventService_KeyUpEvent;
	}

	private async Task OnClick(string itemKey) => await Click.InvokeAsync(itemKey).ConfigureAwait(true);

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
