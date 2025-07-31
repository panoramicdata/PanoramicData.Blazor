namespace PanoramicData.Blazor;

public partial class PDTab : ComponentBase
{
	[CascadingParameter(Name = "TabSet")] public PDTabSet TabSet { get; set; } = default!;

	[Parameter] public Guid Id { get; set; } = Guid.Empty;

	[Parameter] public string Title { get; set; } = string.Empty;

	[Parameter] public RenderFragment? ChildContent { get; set; }

	[Parameter] public string CssClass { get; set; } = string.Empty;

	[Parameter] public bool? IsClosingEnabled { get; set; }

	[Parameter] public bool? IsRenamingEnabled { get; set; }

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
