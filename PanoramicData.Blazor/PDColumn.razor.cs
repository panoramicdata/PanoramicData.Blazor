namespace PanoramicData.Blazor;

public partial class PDColumn<TItem> where TItem : class
{
	private static int _idSequence = 1;

	private string _title = string.Empty;

	private Func<TItem, object>? _compiledFunc;
	private Func<TItem, object>? CompiledFunc => _compiledFunc ??= Field?.Compile();

	public ColumnState State { get; internal set; } = new();

	/// <summary>
	/// Gets or sets the autocomplete attribute value.
	/// </summary>
	[Parameter] public string AutoComplete { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether this column can be shown or hidden by the user.
	/// </summary>
	[Parameter] public bool CanToggleVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets the default sort direction for this column.
	/// </summary>
	[Parameter] public SortDirection DefaultSortDirection { get; set; }

	/// <summary>
	/// Gets or sets a short description of the columns purpose. Overrides DisplayAttribute description if set.
	/// </summary>
	[Parameter] public string? Description { get; set; }

	/// <summary>
	/// Gets or sets a function that returns the description for the field.
	/// </summary>
	/// <remarks>Defaults to Description property if set, otherwise looks for Display attribute.</remarks>
	[Parameter] public Func<FormField<TItem>, PDForm<TItem>?, string> DescriptionFunc { get; set; } = Constants.Functions.FormFieldDescription;

	/// <summary>
	/// Gets or sets optional display options.
	/// </summary>
	[Parameter] public FieldDisplayOptions DisplayOptions { get; set; } = new FieldDisplayOptions();

	/// <summary>
	/// Gets or sets whether this column is editable.
	/// </summary>
	[Parameter] public bool Editable { get; set; } = true;

	/// <summary>
	/// Gets or sets an HTML template for editing.
	/// </summary>
	[Parameter] public RenderFragment<TItem?>? EditTemplate { get; set; }

	/// <summary>
	/// Gets or sets whether this column can be filtered.
	/// </summary>
	[Parameter] public bool Filterable { get; set; }

	/// <summary>
	/// Optional format string for displaying the field value.
	/// </summary>
	[Parameter] public string? Format { get; set; }

	/// <summary>
	/// A Linq expression that selects the field to be data bound to.
	/// </summary>
	[Parameter] public Expression<Func<TItem, object>>? Field { get; set; }

	private PropertyInfo? GetPropertyInfo(object value)
	{
		if (value is MemberExpression memberExpr)
		{
			if (memberExpr.Member is PropertyInfo propInfo)
			{
				return propInfo;
			}
		}
		else if (Field!.Body is UnaryExpression unaryExpr)
		{
			return GetPropertyInfo(unaryExpr.Operand);
		}

		return null;
	}

	/// <summary>
	/// Gets or sets the CSS class for the filter icon.
	/// </summary>
	[Parameter]
	public string FilterIcon { get; set; } = "fas fa-filter";

	public Filter Filter { get; private set; } = new Filter();

	/// <summary>
	/// Gets or sets the key to use for filtering.
	/// </summary>
	[Parameter]
	public string FilterKey { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the options for the filter.
	/// </summary>
	[Parameter]
	public FilterOptions FilterOptions { get; set; } = new();

	/// <summary>
	/// Gets or sets whether to show suggested values in the filter.
	/// </summary>
	[Parameter]
	public bool FilterShowSuggestedValues { get; set; } = true;

	/// <summary>
	/// Gets or sets the suggested values for the filter.
	/// </summary>
	[Parameter]
	public IEnumerable<object> FilterSuggestedValues { get; set; } = [];

	/// <summary>
	/// Gets or sets the maximum number of values to show in the filter.
	/// </summary>
	[Parameter]
	public int? FilterMaxValues { get; set; }

	public string GetPropertyName()
		=> Field != null ? Field.GetPropertyName() : string.Empty;

	/// <summary>
	/// Renders the field value for this column and the given item.
	/// </summary>
	/// <param name="item">The current item.</param>
	/// <returns>The item fields value, formatted if the Format property is set.</returns>
	public string GetRenderValue(TItem item)
	{
		// If the item is null or the field is not set then nothing to output
		if (item is null)
		{
			return string.Empty;
		}

		// Get the value using the compiled and cached function
		try
		{
			var value = CompiledFunc?.Invoke(item);
			if (value is null)
			{
				return string.Empty;
			}

			// password / sensitive info?
			if (IsPassword || IsSensitive(item, null))
			{
				return "".PadRight((value.ToString() ?? string.Empty).Length, '*');
			}

			// if enumeration value - does it have display attribute?
			var memberInfo = Field?.GetPropertyMemberInfo();
			if (memberInfo is PropertyInfo propInfo && propInfo.PropertyType.IsEnum)
			{
				value = propInfo.PropertyType.GetMember($"{value}")
								?.First()
								.GetCustomAttribute<DisplayAttribute>()
								?.Name ?? value;
			}

			// return the string to be rendered
			return string.IsNullOrEmpty(Format)
				? value.ToString() ?? string.Empty
				: string.Format(CultureInfo.CurrentCulture, "{0:" + Format + "}", value);
		}
		catch
		{
			// if field expression is nested member then parent object may be nullable
			return string.Empty;
		}
	}

	/// <summary>
	/// Gets the column value from the given TItem.
	/// </summary>
	/// <param name="item">The TItem for the current row.</param>
	/// <returns>The columns value.</returns>
	public object? GetValue(TItem item) => CompiledFunc?.Invoke(item);

	/// <summary>
	/// Gets or sets an HTML template for the header content.
	/// </summary>
	[Parameter] public RenderFragment? HeaderTemplate { get; set; }

	/// <summary>
	/// Gets or sets an optional helper for filling in the field.
	/// </summary>
	[Parameter] public FormFieldHelper<TItem>? Helper { get; set; }

	/// <summary>
	/// Optional text for the alt attribute of the cell.
	/// </summary>
	[Parameter] public string? HelpText { get; set; }

	/// <summary>
	/// Gets or sets a URL to an external context sensitive help page.
	/// </summary>
	[Parameter] public string? HelpUrl { get; set; }

	/// <summary>
	/// The Id - this should be unique per column in a table
	/// </summary>
	[Parameter] public string Id { get; set; } = $"col-{_idSequence++}";

	/// <summary>
	/// Gets whether this field contains passwords or other sensitive information.
	/// </summary>
	[Parameter] public bool IsPassword { get; set; }

	/// <summary>
	/// Gets or sets a function that determines whether this field contains sensitive values that should not be shown.
	/// </summary>
	[Parameter] public Func<TItem?, PDForm<TItem>?, bool> IsSensitive { get; set; } = Constants.Functions.FormFieldIsSensitive;

	/// <summary>
	/// Gets or sets whether this field contains longer sections of text.
	/// </summary>
	[Parameter] public bool IsTextArea { get; set; }

	/// <summary>
	/// Gets or sets whether the column is visible or not.
	/// </summary>
	/// <remarks>To be visible both this parameter and ShowInList must equal true.</remarks>
	[Parameter] public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets whether this field contains an image
	/// If the field is a string, then the string is treated as the image URL
	/// </summary>
	[Parameter] public bool IsImage { get; set; }

	/// <summary>
	/// Gets or sets the minimum value for numeric values.
	/// </summary>
	[Parameter] public double? MinValue { get; set; }

	/// <summary>
	/// Gets or sets the maximum length for entered text.
	/// </summary>
	[Parameter] public int? MaxLength { get; set; }

	/// <summary>
	/// Gets or sets the maximum value for numeric values.
	/// </summary>
	[Parameter] public double? MaxValue { get; set; }

	/// <summary>
	/// Gets or sets an optional name for the column. Useful for calculated columns that
	/// have no header text / title.
	/// </summary>
	[Parameter] public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets a function that returns available value choices.
	/// </summary>
	[Parameter] public Func<FormField<TItem>, TItem?, OptionInfo[]>? Options { get; set; }

	/// <summary>
	/// Gets an asynchronous function that returns available value choices.
	/// </summary>
	[Parameter] public Func<FormField<TItem>, TItem?, Task<OptionInfo[]>>? OptionsAsync { get; set; }

	/// <summary>
	/// <summary>
	/// Gets or sets the preferred ordinal position of the column (from left to right).
	/// </summary>
	/// <remarks>The default is 1000 for all columns, columns of equal ordinality will remain in their defined order.</remarks>
	[Parameter] public int Ordinal { get; set; } = 1000;

	/// <summary>
	/// Gets or sets the PropertyInfo for the column's field.
	/// </summary>
	public PropertyInfo? PropertyInfo { get; set; }

	/// <summary>
	/// Gets or sets a function that determines whether this field is read-only when the linked form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ReadOnlyInCreate { get; set; } = Constants.Functions.False;

	/// <summary>
	/// Gets or sets a function that determines whether this field is read-only when the linked form mode is Edit.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ReadOnlyInEdit { get; set; } = Constants.Functions.False;

	/// <summary>
	/// Gets or sets whether a 'copy to clipboard' button is displayed for the field.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowCopyButton { get; set; } = Constants.Functions.False;

	/// <summary>
	/// This sets whether something CAN be shown in the list, use DTTable ColumnsToDisplay to dynamically
	/// change which to display from those that CAN be shown in the list
	/// </summary>
	[Parameter] public bool ShowInList { get; set; } = true;

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the linked form mode is Edit.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInEdit { get; set; } = Constants.Functions.True;

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the linked form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInCreate { get; set; } = Constants.Functions.True;

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the linked form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInDelete { get; set; } = Constants.Functions.False;

	/// <summary>
	/// Gets or sets whether the validation result should be shown when displayed in a linked form.
	/// </summary>
	[Parameter] public bool ShowValidationResult { get; set; } = true;

	public void SetId(string id)
	{
		Id = id;
	}

	public void SetValue(TItem item, object? value)
	{
		// a null Field represents a calculated / display only column
		if (Field != null)
		{
			var propInfo = GetPropertyInfo(Field!.Body)
				?? throw new PDTableException("Unable to determine column data type from Field expression");

			if (propInfo.PropertyType.IsAssignableFrom(value?.GetType()))
			{
				propInfo.SetValue(item, value);
			}
			else
			{
				var stringValue = value?.ToString() ?? string.Empty;
				TypeConverter typeConverter = TypeDescriptor.GetConverter(propInfo.PropertyType);
				object? propValue = typeConverter.ConvertFromString(stringValue);
				propInfo.SetValue(item, propValue);
			}
		}
	}

	/// <summary>
	/// Gets or sets whether this column can be sorted.
	/// </summary>
	[Parameter] public bool Sortable { get; set; } = true;

	/// <summary>
	/// Gets or sets the current sort direction of this column.
	/// </summary>
	public SortDirection SortDirection { get; set; }

	/// <summary>
	/// Returns the markup to represent the current sort state.
	/// </summary>
	public string SortIcon
	{
		get
		{
			var html = SortDirection switch
			{
				SortDirection.Ascending => "<i class=\"ms-1 pd-sort fas fa-sort-up fa-stack-1x\"></i>",
				SortDirection.Descending => "<i class=\"ms-1 pd-sort fas fa-sort-down fa-stack-1x\"></i>",
				_ => ""
			};
			return $"<span class=\"fa-stack\"><i class=\"ms-1 pd-sort fas fa-sort fa-stack-1x\"></i>{html}</span>";
		}
	}

	/// <summary>
	/// The parent PDTable instance.
	/// </summary>
	[CascadingParameter(Name = "Table")]
	public PDTable<TItem> Table { get; set; } = null!;

	/// <summary>
	/// Optional CSS class for the column cell.
	/// </summary>
	[Parameter] public string? TdClass { get; set; }

	/// <summary>
	/// Optional CSS class for the column header.
	/// </summary>
	[Parameter] public string? ThClass { get; set; }

	/// <summary>
	/// If set will override the FieldExpression's name
	/// </summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>
	/// Gets or sets an HTML template for the fields value.
	/// </summary>
	[Parameter] public RenderFragment<TItem>? Template { get; set; }

	/// <summary>
	/// Gets or sets the number of rows of text displayed by default in a text area.,
	/// </summary>
	[Parameter] public int TextAreaRows { get; set; } = 4;

	/// <summary>
	/// The data type of the columns field value.
	/// </summary>
	[Parameter] public Type? Type { get; set; }

	/// <summary>
	/// Gets or sets whether the contents of this cell are user selectable.
	/// </summary>
	/// <remarks>When set to a null (default) will use Table property value.</remarks>
	[Parameter] public bool? UserSelectable { get; set; }

	public string GetTitle()
	{
		if (Title != null)
		{
			return Title;
		}

		var memberInfo = Field?.GetPropertyMemberInfo();
		return memberInfo is PropertyInfo propInfo
			? propInfo.GetCustomAttribute<DisplayAttribute>()?.Name ?? propInfo.Name
			: memberInfo?.Name ?? string.Empty;
	}

	protected override async Task OnInitializedAsync()
	{
		if (Table == null)
		{
			throw new InvalidOperationException($"Error initializing column {Id}. " +
				"Table reference is null which implies it did not initialize or that the column " +
				$"type '{typeof(TItem)}' does not match the table type.");
		}

		// default state
		State.Visible = IsVisible;
		State.Ordinal = Ordinal;

		// register with table
		await Table.AddColumnAsync(this).ConfigureAwait(true);
	}

	protected override void OnParametersSet()
	{
		// Validate that enough parameters have been set correctly
		if (Type == null)
		{
			Type = Field?.GetPropertyMemberInfo()?.GetMemberUnderlyingType();
		}

		PropertyInfo = typeof(TItem).GetProperties().SingleOrDefault(p => p.Name == Field?.GetPropertyMemberInfo()?.Name);
	}

	public void SetOrdinal(int ordinal)
	{
		State.Ordinal = ordinal;
		StateHasChanged();
	}

	public void SetShowInList(bool showInList)
	{
		ShowInList = showInList;
		StateHasChanged();
	}

	public void SetTitle(string title)
	{
		_title = title;
		StateHasChanged();
	}

	public void SetVisible(bool isVisible)
	{
		State.Visible = isVisible;
		StateHasChanged();
	}

	public FilterDataTypes GetFilterDataType()
	{
		var memberInfo = Field?.GetPropertyMemberInfo();

		if (Field is MemberExpression me)
		{
			if (Nullable.GetUnderlyingType(me.Type) != null)
			{
				// If the member expression type is nullable, return its underlying type
				var t = Nullable.GetUnderlyingType(me.Type);

			}
		}


		if (memberInfo is PropertyInfo propInfo)
		{
			// nullable?
			var ut = Nullable.GetUnderlyingType(propInfo.PropertyType);

			if (propInfo.PropertyType.IsEnum || ut?.IsEnum == true)
			{
				return FilterDataTypes.Enum;
			}

			if (propInfo.PropertyType.FullName == "System.Boolean" || ut?.FullName == "System.Boolean")
			{
				return FilterDataTypes.Bool;
			}

			if (propInfo.PropertyType.FullName == "System.String")
			{
				return FilterDataTypes.Text;
			}

			if (propInfo.PropertyType.FullName == "System.DateTime" || propInfo.PropertyType.FullName == "System.DateTimeOffset"
				 || ut?.FullName == "System.DateTime" || ut?.FullName == "System.DateTimeOffset")
			{
				return FilterDataTypes.Date;
			}
		}

		return FilterDataTypes.Numeric;
	}

	public bool GetFilterIsNullable()
	{
		var memberInfo = Field?.GetPropertyMemberInfo();
		if (memberInfo is PropertyInfo propInfo)
		{
			if (propInfo.PropertyType.FullName == "System.String")
			{
				return true;
			}

			return Nullable.GetUnderlyingType(propInfo.PropertyType) != null;
		}

		return false;
	}

	public string GetFilterKey()
	{
		if (Field != null)
		{
			return FilterKeyVisitor.GetFilterKey(Field);
		}

		return Id;
	}

	private class FilterKeyVisitor(Expression parameter) : ExpressionVisitor
	{
		public string FilterKey { get; private set; } = string.Empty;

		public override Expression? Visit(Expression? node)
		{
			if (node != null)
			{
				var chain = node.MemberClauses().ToList();
				if (chain.Count != 0 && chain.First().Expression == parameter)
				{
					FilterKey = string.Join(".", chain.Select(
						mexpr => mexpr.Member.GetCustomAttribute<FilterKeyAttribute>()?.Value
								?? mexpr.Member.GetCustomAttribute<DisplayAttribute>()?.ShortName
								?? mexpr.Member.Name.LowerFirstChar()
					));
					return node;
				}
			}

			return base.Visit(node);
		}

		public static string GetFilterKey(Expression<Func<TItem, object>> expr)
		{
			var visitor = new FilterKeyVisitor(expr.Parameters[0]);
			visitor.Visit(expr);
			return visitor.FilterKey;
		}

	}
}