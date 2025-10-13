namespace PanoramicData.Blazor;

public partial class PDPager : IDisposable, IEnablable
{
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

	protected override void OnInitialized() => PageCriteria.TotalCountChanged += PageCriteria_TotalCountChanged;

	public void Dispose()
	{
		PageCriteria.TotalCountChanged -= PageCriteria_TotalCountChanged;
		GC.SuppressFinalize(this);
	}

	private void PageCriteria_TotalCountChanged(object? sender, EventArgs e) => StateHasChanged();

	public void MoveLast() => PageCriteria.Page = PageCriteria.PageCount;

	public void MoveNext() => PageCriteria.Page++;

	public void MovePrevious() => PageCriteria.Page--;

	public void MoveFirst() => PageCriteria.Page = 1;

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

	private string ControlSizeCssClass
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
}
