namespace PanoramicData.Blazor;

public partial class PDColumn<TItem> where TItem : class
{
	private static int _idSequence = 1;
	private string? _title;
	private Func<TItem, object>? _compiledFunc;
	private Func<TItem, object>? CompiledFunc => _compiledFunc ??= Field?.Compile();

	/// <summary>
	/// Gets or sets the autocomplete attribute value.
	/// </summary>
	[Parameter] public string AutoComplete { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the default sort direction for this column.
	/// </summary>
	[Parameter] public SortDirection DefaultSortDirection { get; set; }

	/// <summary>
	/// Gets or sets a short description of the columns purpose. Overrides DisplayAttribute description if set.
	/// </summary>
	[Parameter] public string? Description { get; set; }

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

	[Parameter]
	public string FilterIcon { get; set; } = "fas fa-filter";

	public Filter Filter { get; private set; } = new Filter();

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
		var value = CompiledFunc?.Invoke(item);
		if (value == null)
		{
			return string.Empty;
		}

		// password / sensitive info?
		if (IsPassword || IsSensitive(item, null))
		{
			return "".PadRight(value.ToString().Length, '*');
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
			? value.ToString()
			: string.Format(CultureInfo.CurrentCulture, "{0:" + Format + "}", value);
	}

	/// <summary>
	/// Gets the column value from the given TItem.
	/// </summary>
	/// <param name="item">The TItem for the current row.</param>
	/// <returns>The columns value.</returns>
	public object? GetValue(TItem item)
	{
		return CompiledFunc?.Invoke(item);
	}

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
	[Parameter] public Func<TItem?, PDForm<TItem>?, bool> IsSensitive { get; set; } = new Func<TItem?, PDForm<TItem>?, bool>((_, __) => false);

	/// <summary>
	/// Gets or sets whether this field contains longer sections of text.
	/// </summary>
	[Parameter] public bool IsTextArea { get; set; }

	/// <summary>
	/// Gets or sets the maximum length for entered text.
	/// </summary>
	[Parameter] public int? MaxLength { get; set; }

	/// <summary>
	/// Gets a function that returns available value choices.
	/// </summary>
	[Parameter] public Func<FormField<TItem>, TItem?, OptionInfo[]>? Options { get; set; }

	/// <summary>
	/// Gets an asynchronous function that returns available value choices.
	/// </summary>
	[Parameter] public Func<FormField<TItem>, TItem?, Task<OptionInfo[]>>? OptionsAsync { get; set; }

	/// <summary>
	/// Gets or sets the attributes of the underlying property.
	/// </summary>
	public PropertyInfo? PropertyInfo { get; set; }

	/// <summary>
	/// Gets or sets a function that determines whether this field is read-only when the linked form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ReadOnlyInCreate { get; set; } = new Func<TItem?, bool>((_) => false);

	/// <summary>
	/// Gets or sets a function that determines whether this field is read-only when the linked form mode is Edit.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ReadOnlyInEdit { get; set; } = new Func<TItem?, bool>((_) => false);

	/// <summary>
	/// This sets whether something CAN be shown in the list, use DTTable ColumnsToDisplay to dynamically
	/// change which to display from those that CAN be shown in the list
	/// </summary>
	[Parameter] public bool ShowInList { get; set; } = true;

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the linked form mode is Edit.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInEdit { get; set; } = new Func<TItem?, bool>((_) => true);

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the linked form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInCreate { get; set; } = new Func<TItem?, bool>((_) => true);

	/// <summary>
	/// Gets or sets a function that determines whether this field is visible when the linked form mode is Create.
	/// </summary>
	[Parameter] public Func<TItem?, bool> ShowInDelete { get; set; } = new Func<TItem?, bool>((_) => false);

	/// <summary>
	/// Gets or sets whether the validation result should be shown when displayed in a linked form.
	/// </summary>
	[Parameter] public bool ShowValidationResult { get; set; } = true;

	public void SetValue(TItem item, object? value)
	{
		// a null Field represents a calculated / display only column
		if (Field != null)
		{
			var propInfo = GetPropertyInfo(Field!.Body);
			if (propInfo == null)
			{
				throw new PDTableException("Unable to determine column data type from Field expression");
			}
			if (propInfo.PropertyType.IsAssignableFrom(value?.GetType()))
			{
				propInfo.SetValue(item, value);
			}
			else
			{
				var stringValue = value?.ToString();
				TypeConverter typeConverter = TypeDescriptor.GetConverter(propInfo.PropertyType);
				object propValue = typeConverter.ConvertFromString(stringValue);
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
				SortDirection.Ascending => "<i class=\"ml-1 pd-sort fas fa-sort-up fa-stack-1x\"></i>",
				SortDirection.Descending => "<i class=\"ml-1 pd-sort fas fa-sort-down fa-stack-1x\"></i>",
				_ => ""
			};
			return $"<span class=\"fa-stack\"><i class=\"ml-1 pd-sort fas fa-sort fa-stack-1x\"></i>{html}</span>";
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
	public string Title
	{
		get
		{
			if (_title == null)
			{
				var memberInfo = Field?.GetPropertyMemberInfo();
				if (memberInfo is PropertyInfo propInfo)
				{
					_title = propInfo.GetCustomAttribute<DisplayAttribute>()?.Name ?? propInfo.Name;
				}
				else
				{
					_title = memberInfo?.Name;
				}
			}
			return _title ?? "";
		}
		set { _title = value; }
	}

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

	protected override async Task OnInitializedAsync()
	{
		if (Table == null)
		{
			throw new InvalidOperationException($"Error initializing column {Id}. " +
				"Table reference is null which implies it did not initialize or that the column " +
				$"type '{typeof(TItem)}' does not match the table type.");
		}
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

	public FilterDataTypes GetFilterDataType()
	{
		var memberInfo = Field?.GetPropertyMemberInfo();
		if (memberInfo is PropertyInfo propInfo)
		{
			if (propInfo.PropertyType.IsEnum)
			{
				return FilterDataTypes.Enum;
			}
			if (propInfo.PropertyType.FullName == "System.String")
			{
				return FilterDataTypes.Text;
			}
			if (propInfo.PropertyType.FullName == "System.DateTime" || propInfo.PropertyType.FullName == "System.DateTimeOffset")
			{
				return FilterDataTypes.Date;
			}
		}
		return FilterDataTypes.Numeric;
	}
}