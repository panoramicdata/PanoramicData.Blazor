namespace PanoramicData.Blazor;

/// <summary>
/// Groups a block of <see cref="PDColumn{TItem}"/> definitions under a named facet for use with
/// <see cref="PDColumnGrouper{TItem}"/>. This is an optional convenience over setting <c>Group</c> on each
/// column: it sets the group name once for every wrapped column and carries the facet's icon, order and
/// tooltip. It renders no markup of its own.
/// </summary>
public partial class PDColumnGroup
{
	private readonly ColumnGroupContext _context = new();

	/// <summary>
	/// Gets or sets the unique name of the column group. Also used as the facet pill label.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets an optional icon CSS class shown on the facet pill (e.g. "fas fa-chart-bar").
	/// </summary>
	[Parameter]
	public string? Icon { get; set; }

	/// <summary>
	/// Gets or sets the order in which the facet pill appears. Lower values appear first.
	/// </summary>
	[Parameter]
	public int Ordinal { get; set; } = 1000;

	/// <summary>
	/// Gets or sets an optional description used as the facet pill tooltip.
	/// </summary>
	[Parameter]
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets the columns that belong to this group.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment? ChildContent { get; set; }

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		// Keep the cascaded context (a stable reference) in sync with the parameters.
		_context.Name = Name;
		_context.Icon = Icon;
		_context.Ordinal = Ordinal;
		_context.Description = Description;
	}
}
