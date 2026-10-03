namespace PanoramicData.Blazor;

public partial class PDTable<TItem>
{
	/// <summary>
	/// Adds the given column to the list of available columns.
	/// </summary>
	/// <param name="column">The PDColumn to be added.</param>
	public async Task AddColumnAsync(PDColumn<TItem> column)
	{
		try
		{
			ImproveDefaultColumnId(column);

			Columns.Add(column);
			if (IsSortColumn(column))
			{
				column.SortDirection = SortCriteria!.Direction;
			}

			ConfigureColumnFilter(column);

			StateHasChanged();
			RaiseGroupsChanged();
		}
		catch (Exception ex)
		{
			await HandleExceptionAsync(ex).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Replaces a default column id (col-1, col-2, ...) with one derived from the column name or title,
	/// which makes persisted column state stable.
	/// </summary>
	private static void ImproveDefaultColumnId(PDColumn<TItem> column)
	{
		if (ColumnIdRegex().IsMatch(column.Id))
		{
			var name = string.IsNullOrEmpty(column.Name) ? column.GetTitle() : column.Name;
			if (!string.IsNullOrWhiteSpace(name))
			{
				var simpleName = name.ExtractAlphanumericChars().ToLower(CultureInfo.InvariantCulture);
				if (!string.IsNullOrWhiteSpace(simpleName))
				{
					column.SetId($"col-{simpleName}");
				}
			}
		}
	}

	/// <summary>
	/// Sets up the filter key and property name of a filterable column, and registers the key with the
	/// data provider's mappings.
	/// </summary>
	private void ConfigureColumnFilter(PDColumn<TItem> column)
	{
		if (column.Filterable && column.Field != null)
		{
			// obtain filter key
			column.Filter.Key = string.IsNullOrWhiteSpace(column.FilterKey) ? column.GetFilterKey() : column.FilterKey;

			// determine property name
			column.Filter.PropertyName = column.GetPropertyName();

			// update mapping - only set if not already explicitly mapped by the data provider,
			// as the ViewModel property name may differ from the entity property path
			// (e.g. TenantName on the VM vs Tenant.Name as a navigation property path)
			if (DataProvider is IFilterProviderService<TItem> fs && !fs.KeyPropertyMappings.ContainsKey(column.Filter.Key))
			{
				fs.KeyPropertyMappings[column.Filter.Key] = column.Filter.PropertyName;
			}
		}
	}

	#region Column groups

	/// <summary>
	/// Gets the column groups registered via <see cref="PDColumnGroup"/> wrappers, in registration order.
	/// </summary>
	public List<ColumnGroupContext> ColumnGroups { get; } = [];

	/// <summary>
	/// Gets the active column group facet, or null when all groups are shown. This is transient view state
	/// and is intentionally not persisted with column visibility.
	/// </summary>
	public string? ActiveColumnGroup { get; private set; }

	/// <summary>
	/// Raised when a column or column group is registered, so a <see cref="PDColumnGrouper{TItem}"/> can
	/// refresh its facets.
	/// </summary>
	public event EventHandler? GroupsChanged;

	/// <summary>
	/// Sets the active column group facet. Columns whose group does not match are hidden; ungrouped columns
	/// remain visible. Pass null or empty to show all groups.
	/// </summary>
	/// <param name="group">The group to show, or null/empty for all.</param>
	public void SetActiveColumnGroup(string? group)
	{
		ActiveColumnGroup = string.IsNullOrEmpty(group) ? null : group;
		StateHasChanged();
	}

	/// <summary>
	/// Registers column group metadata (icon, order, tooltip) declared by a <see cref="PDColumnGroup"/>.
	/// Ignored when a group with the same name is already registered.
	/// </summary>
	/// <param name="context">The column group metadata to register.</param>
	public void RegisterColumnGroup(ColumnGroupContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		if (string.IsNullOrEmpty(context.Name))
		{
			return;
		}

		if (!ColumnGroups.Any(g => string.Equals(g.Name, context.Name, StringComparison.Ordinal)))
		{
			ColumnGroups.Add(context);
			RaiseGroupsChanged();
		}
	}

	/// <summary>
	/// Raises <see cref="GroupsChanged"/> for any subscribers.
	/// </summary>
	private void RaiseGroupsChanged()
	{
		var handler = GroupsChanged;
		handler?.Invoke(this, EventArgs.Empty);
	}

	#endregion

	/// <summary>
	/// Determines whether the given column is the one named by the current sort key, by id or by title. An
	/// empty or null key means no sort has been chosen, so it matches no column, not even one with an empty title.
	/// </summary>
	private bool IsSortColumn(PDColumn<TItem> column)
	{
		var key = SortCriteria?.Key;
		return !string.IsNullOrEmpty(key) && (column.Id == key || column.GetTitle() == key);
	}

	private string GetDynamicCellClasses(PDColumn<TItem> col)
	{
		var sb = new StringBuilder();
		sb.Append(col.TdClass);
		sb.Append(' ');
		if ((col.UserSelectable ?? UserSelectable) == false)
		{
			sb.Append("noselect ");
		}

		return sb.ToString().Trim();
	}

	private string GetDynamicRowClasses(TItem item)
	{
		var sb = new StringBuilder();
		if (IsSelected(item))
		{
			sb.Append("selected ");
		}

		if (!RowIsEnabled(item))
		{
			sb.Append("disabled ");
		}

		if (RowClass != null)
		{
			var classes = RowClass(item);
			if (!string.IsNullOrWhiteSpace(classes))
			{
				sb.Append(classes);
			}
		}

		return sb.ToString().Trim();
	}

	[GeneratedRegex(@"^col-\d+$")]
	private static partial Regex ColumnIdRegex();
}
