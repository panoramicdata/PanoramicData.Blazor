namespace PanoramicData.Blazor;

/// <summary>
/// Displays paging controls and page-size selection for paged datasets.
/// </summary>
public partial class PDPager : IDisposable, IEnablable
{
	/// <summary>
	/// Additional CSS that can be applied to a pager component.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Determines whether the component is enabled or not.
	/// </summary>
	[Parameter] public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the text to be displayed when no items are available.
	/// </summary>
	[Parameter] public string NoItemsText { get; set; } = "No items to display";

	/// <summary>
	/// Sets the initial page count.
	/// </summary>
	[Parameter] public PageCriteria PageCriteria { get; set; } = new PageCriteria(1, 10, 0);

	/// <summary>
	/// Gets or sets the possible page sizes offered to the user.
	/// </summary>
	[Parameter] public uint[] PageSizeChoices { get; set; } = [10, 25, 50, 100, 250, 500];

	/// <summary>
	/// Determines whether the navigation buttons are displayed.
	/// </summary>
	[Parameter] public bool ShowPageChangeButtons { get; set; } = true;

	/// <summary>
	/// Determines whether the description of the current page items is displayed.
	/// </summary>
	[Parameter] public bool ShowPageDescription { get; set; } = true;

	/// <summary>
	/// Determines whether the page size choices are displayed.
	/// </summary>
	[Parameter] public bool ShowPageSizeChoices { get; set; } = true;

	/// <summary>
	/// Gets or sets the button sizes.
	/// </summary>
	[Parameter] public ButtonSizes? Size { get; set; }

	/// <summary>
	/// Subscribes to page criteria updates.
	/// </summary>
	protected override void OnInitialized() => PageCriteria.TotalCountChanged += PageCriteria_TotalCountChanged;

	/// <summary>
	/// Unsubscribes event handlers used by the pager.
	/// </summary>
	public void Dispose()
	{
		PageCriteria.TotalCountChanged -= PageCriteria_TotalCountChanged;
		GC.SuppressFinalize(this);
	}

	private void PageCriteria_TotalCountChanged(object? sender, EventArgs e) => StateHasChanged();

	/// <summary>
	/// Navigates to the last page.
	/// </summary>
	public void MoveLast() => PageCriteria.Page = PageCriteria.PageCount;

	/// <summary>
	/// Navigates to the next page.
	/// </summary>
	public void MoveNext() => PageCriteria.Page++;

	/// <summary>
	/// Navigates to the previous page.
	/// </summary>
	public void MovePrevious() => PageCriteria.Page--;

	/// <summary>
	/// Navigates to the first page.
	/// </summary>
	public void MoveFirst() => PageCriteria.Page = 1;

	/// <summary>
	/// Enables pager interactions.
	/// </summary>
	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	/// <summary>
	/// Disables pager interactions.
	/// </summary>
	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	/// <summary>
	/// Sets whether pager interactions are enabled.
	/// </summary>
	/// <param name="isEnabled">True to enable interaction; otherwise false.</param>
	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}

	private string ControlSizeCssClass
	{
		get
		{
			return Size switch
			{
				ButtonSizes.Small => "form-select-sm",
				ButtonSizes.Large => "form-select-lg",
				_ => string.Empty,
			};
		}
	}
}
