namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a single tab within a <see cref="PDTabSet"/>.
/// </summary>
public partial class PDTab : ComponentBase, IDisposable
{
	/// <summary>
	/// Gets or sets the parent tab set.
	/// </summary>
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
	/// Gets or sets an icon CSS class to display before the tab title (e.g. "bi bi-star" for Bootstrap Icons).
	/// </summary>
	[Parameter] public string? IconCssClass { get; set; }

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

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		if (Id == Guid.Empty)
		{
			Id = Guid.NewGuid();
		}

		TempTitle = Title;
		TabSet?.AddTab(this);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		// Keep TempTitle in sync with Title if not renaming
		if (!IsRenaming)
		{
			TempTitle = Title;
		}
	}

	/// <summary>
	/// Gets the display title of the tab.
	/// </summary>
	public string GetTitle() => Title;

	/// <summary>
	/// Gets the child content of the tab.
	/// </summary>
	public RenderFragment? GetChildContent() => ChildContent;

	/// <summary>
	/// Removes this tab from its parent when it leaves the render tree.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A tab registers itself with its <see cref="PDTabSet"/> in <see cref="OnInitialized"/>, so without the
	/// matching removal here it stayed in the parent's list for ever. That is invisible for the declarative
	/// case these components were written for - a fixed set of tabs written out in markup, none of which ever
	/// leaves - and wrong the moment tabs are rendered from a collection that changes: removing an item
	/// disposed its <see cref="PDTab"/> while the parent went on rendering a tab for it, so a closed tab
	/// stayed in the strip and clicking it showed content belonging to nothing.
	/// </para>
	/// <para>
	/// Removal is deliberately not a close: <see cref="PDTabSet.OnTabClosed"/> reports a user closing a tab,
	/// and raising it here would fire again for every tab on the way down when the whole tab set is disposed.
	/// </para>
	/// </remarks>
	public void Dispose()
	{
		TabSet?.RemoveTab(this);
		GC.SuppressFinalize(this);
	}
}
