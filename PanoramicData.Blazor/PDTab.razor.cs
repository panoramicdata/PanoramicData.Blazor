namespace PanoramicData.Blazor;

public partial class PDTab : ComponentBase
{
	[CascadingParameter(Name = "TabSet")] public PDTabSet TabSet { get; set; } = default!;

	/// <summary>
	/// Gets or sets the unique identifier for the tab.
	/// </summary>
	[Parameter] public Guid Id { get; set; } = Guid.Empty;

	/// <summary>
	/// Gets or sets the title of the tab.
	/// </summary>
	[Parameter] public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the child content of the tab.
	/// </summary>
	[Parameter] public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the tab.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the tab can be closed. This overrides the parent TabSet's setting.
	/// </summary>
	[Parameter] public bool? IsClosingEnabled { get; set; }

	/// <summary>
	/// Gets or sets whether the tab can be renamed. This overrides the parent TabSet's setting.
	/// </summary>
	[Parameter] public bool? IsRenamingEnabled { get; set; }

	/// <summary>
	/// An event callback that is invoked when the tab is selected.
	/// </summary>
	[Parameter] public EventCallback OnSelected { get; set; }

	// Internal state for renaming
	internal bool IsRenaming { get; set; }
	internal string TempTitle { get; set; } = string.Empty;

	protected override void OnInitialized()
	{
		if (Id == Guid.Empty)
		{
			Id = Guid.NewGuid();
		}

		TempTitle = Title;
		TabSet?.AddTab(this);
	}

	protected override void OnParametersSet()
	{
		// Keep TempTitle in sync with Title if not renaming
		if (!IsRenaming)
		{
			TempTitle = Title;
		}
	}

	public string GetTitle() => Title;
	public RenderFragment? GetChildContent() => ChildContent;
}
