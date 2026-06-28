namespace PanoramicData.Blazor;

/// <summary>
/// Renders a single-select set of facet pills for the column groups of a <see cref="PDTable{TItem}"/>,
/// letting the user switch which group of columns is shown. Place it anywhere — it takes the table by
/// reference, so it is commonly positioned above the table in its own row.
/// </summary>
/// <typeparam name="TItem">Row item type of the target table.</typeparam>
public partial class PDColumnGrouper<TItem> : IDisposable where TItem : class
{
	private PDTable<TItem>? _subscribedTable;

	/// <summary>
	/// Gets or sets the table whose column groups are presented. Supply the table's <c>@ref</c>.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public PDTable<TItem> Table { get; set; } = null!;

	/// <summary>
	/// Gets or sets the visual style of the facet control.
	/// </summary>
	[Parameter]
	public PDColumnGroupVariant Variant { get; set; } = PDColumnGroupVariant.Segmented;

	/// <summary>
	/// Gets or sets whether each facet shows a count of the columns it contains.
	/// </summary>
	[Parameter]
	public bool ShowCounts { get; set; }

	/// <summary>
	/// Gets or sets whether a leading facet that shows all columns is displayed.
	/// </summary>
	[Parameter]
	public bool ShowAllPill { get; set; } = true;

	/// <summary>
	/// Gets or sets the label of the "all columns" facet.
	/// </summary>
	[Parameter]
	public string AllText { get; set; } = "All";

	/// <summary>
	/// Gets or sets an optional icon CSS class for the "all columns" facet.
	/// </summary>
	[Parameter]
	public string? AllIcon { get; set; }

	/// <summary>
	/// Gets or sets additional CSS classes applied to each facet pill.
	/// </summary>
	[Parameter]
	public string? PillCssClass { get; set; }

	/// <summary>
	/// Gets or sets additional CSS classes applied to the active facet pill.
	/// </summary>
	[Parameter]
	public string? ActivePillCssClass { get; set; }

	/// <summary>
	/// Gets or sets additional CSS classes applied to the text within each facet pill.
	/// </summary>
	[Parameter]
	public string? TextCssClass { get; set; }

	/// <summary>
	/// Gets or sets additional CSS classes applied to the icon within each facet pill.
	/// </summary>
	[Parameter]
	public string? IconCssClass { get; set; }

	private string VariantCssClass => Variant == PDColumnGroupVariant.Segmented ? "segmented" : "pills";

	private bool IsActive(string? group)
		=> string.Equals(Table?.ActiveColumnGroup ?? string.Empty, group ?? string.Empty, StringComparison.Ordinal);

	private List<RenderPill> GetRenderPills()
	{
		var result = new List<RenderPill>();
		if (Table is null)
		{
			return result;
		}

		if (ShowAllPill)
		{
			var total = Table.Columns.Count(c => c.ShowInList);
			result.Add(new RenderPill(null, AllText, AllIcon, null, total));
		}

		var listableGroups = Table.Columns.Where(c => c.ShowInList).Select(c => c.GroupName);
		foreach (var pill in ColumnGroupHelper.BuildPills(Table.ColumnGroups, listableGroups))
		{
			result.Add(new RenderPill(pill.Name, pill.Name, pill.Icon, pill.Description, pill.Count));
		}

		return result;
	}

	private void OnPillClicked(string? group)
	{
		Table?.SetActiveColumnGroup(group);
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// The table is supplied by @ref and may arrive (or change) after the first render; (re)subscribe
		// so the facets refresh as columns and groups register.
		if (!ReferenceEquals(_subscribedTable, Table))
		{
			if (_subscribedTable is not null)
			{
				_subscribedTable.GroupsChanged -= OnTableGroupsChanged;
			}

			_subscribedTable = Table;

			if (_subscribedTable is not null)
			{
				_subscribedTable.GroupsChanged += OnTableGroupsChanged;
			}
		}
	}

	private void OnTableGroupsChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);

	/// <inheritdoc />
	public void Dispose()
	{
		if (_subscribedTable is not null)
		{
			_subscribedTable.GroupsChanged -= OnTableGroupsChanged;
		}

		GC.SuppressFinalize(this);
	}

	private sealed record RenderPill(string? Group, string Text, string? Icon, string? Description, int Count);
}
