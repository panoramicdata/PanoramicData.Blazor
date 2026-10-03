namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a configurable filter UI for a single data column.
/// </summary>
public partial class PDFilter : IAsyncDisposable
{
	private static int _sequence;
	private readonly string _id = $"filter-button-{(++_sequence)}";
	private string[] _values = [];
	private string _value1 = string.Empty;
	private string _value2 = string.Empty;
	private FilterTypes _filterType = FilterTypes.Equals;
	private string _valuesFilter = string.Empty;
	private readonly List<string> _selectedValues = [];
	private IJSObjectReference? _commonModule;
	private static readonly char[] _separator = ['|'];

	// operators that compare with a single selected value (or, for a range, the first of two)
	private static readonly FilterTypes[] _singleValueTypes = [FilterTypes.Equals, FilterTypes.DoesNotEqual, FilterTypes.GreaterThan, FilterTypes.GreaterThanOrEqual, FilterTypes.LessThan, FilterTypes.LessThanOrEqual, FilterTypes.Range];

	// operators that never change to a multiple value operator when a further value is clicked
	private static readonly FilterTypes[] _singleOnlyTypes = [FilterTypes.GreaterThan, FilterTypes.GreaterThanOrEqual, FilterTypes.LessThan, FilterTypes.LessThanOrEqual];

	/// <summary>
	/// Gets the drop down holding the filter editor, set by the markup.
	/// </summary>
	internal PDDropDown DropDown { get; set; } = null!;

	/// <summary>
	/// Gets the injected JavaScript runtime.
	/// </summary>
	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the CSS class for the component.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = "p-0 ms-1";

	/// <summary>
	/// Gets or sets the filter object.
	/// </summary>
	[Parameter]
	public Filter Filter { get; set; } = new Filter();

	/// <summary>
	/// An event callback that is invoked when the filter changes.
	/// </summary>
	[Parameter]
	public EventCallback<Filter> FilterChanged { get; set; }

	/// <summary>
	/// A function to fetch the values for the filter.
	/// </summary>
	[Parameter]
	public Func<Filter, Task<string[]>>? FetchValuesAsync { get; set; }

	/// <summary>
	/// An optional function that transforms a raw filter value into a display label.
	/// The raw value is still used for filtering; only the displayed text changes.
	/// </summary>
	[Parameter]
	public Func<string, string>? FilterValueDisplayFunc { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the icon.
	/// </summary>
	[Parameter]
	public string IconCssClass { get; set; } = "fas fa-filter";

	/// <summary>
	/// Gets or sets the data type for the filter.
	/// </summary>
	[Parameter]
	public FilterDataTypes DataType { get; set; }

	/// <summary>
	/// Gets or sets whether the value can be null.
	/// </summary>
	[Parameter]
	public bool Nullable { get; set; }

	/// <summary>
	/// Gets or sets the filter options.
	/// </summary>
	[Parameter]
	public FilterOptions Options { get; set; } = new();

	/// <summary>
	/// Gets or sets whether to show the values for the filter.
	/// </summary>
	[Parameter]
	public bool ShowValues { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the select all / deselect all row above the values list.
	/// </summary>
	[Parameter]
	public bool ShowSelectAll { get; set; }

	/// <summary>
	/// Gets or sets the size of the filter button.
	/// </summary>
	[Parameter]
	public ButtonSizes Size { get; set; } = ButtonSizes.Small;


	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
			// the module may already be gone with the circuit, and there is nothing left to release
		}
	}

	private bool HasFilter => Filter.FilterType switch
	{
		FilterTypes.IsNull => true,
		FilterTypes.IsNotNull => true,
		FilterTypes.IsEmpty => true,
		FilterTypes.IsNotEmpty => true,
		_ => !string.IsNullOrWhiteSpace(Filter.Value)
	};

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_commonModule = await JSRuntime
					.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl)
					.ConfigureAwait(true);
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	private async Task OnClear()
	{
		_filterType = FilterTypes.Equals;
		_value1 = string.Empty;
		_value2 = string.Empty;
		_selectedValues.Clear();
		Filter.Clear();
		await DropDown.HideAsync().ConfigureAwait(true);
		await FilterChanged.InvokeAsync(Filter).ConfigureAwait(true);
	}

	private void OnSelectAllClicked()
	{
		if (_selectedValues.Count == _values.Length)
		{
			// all selected -> deselect all
			_selectedValues.Clear();
			_value1 = string.Empty;
		}
		else
		{
			// none or some selected -> select all visible
			_selectedValues.Clear();
			_selectedValues.AddRange(_values);
			_filterType = FilterTypes.In;
			_value1 = JoinSelectedValues();
		}
	}

	private async Task OnDropDownShown()
	{
		_selectedValues.Clear();
		_filterType = Filter.FilterType;
		_value1 = Filter.Value;
		_value2 = Filter.Value2;
		await RefreshValues().ConfigureAwait(true);
		// Add selected values for NotIn, Equals and NotEquals
		if (_filterType is FilterTypes.In 
			or FilterTypes.NotIn 
			or FilterTypes.Equals 
			or FilterTypes.DoesNotEqual
			or FilterTypes.GreaterThan
			or FilterTypes.GreaterThanOrEqual
			or FilterTypes.LessThan
			or FilterTypes.LessThanOrEqual)
		{
			_selectedValues.AddRange([.. _value1.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())]);
		}

		if (_filterType == FilterTypes.Range)
		{
			_selectedValues.AddRange([.. _value1.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())]);
			_selectedValues.AddRange([.. _value2.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())]);
		}
	}

	private async Task OnDropDownKeyPress(int keyCode)
	{
		if (keyCode == 13 && _commonModule != null)
		{
			// can happen before lost focus and hence text value not updated
			// so force focus to filter button and perform click
			await _commonModule.InvokeVoidAsync("focus", _id).ConfigureAwait(true);
			await _commonModule.InvokeVoidAsync("click", _id).ConfigureAwait(true);
		}
	}

	private async Task OnFilter()
	{
		Filter.FilterType = _filterType;
		Filter.Value = _value1;
		Filter.Value2 = _value2;
		await DropDown.HideAsync().ConfigureAwait(true);
		await FilterChanged.InvokeAsync(Filter).ConfigureAwait(true);
	}

	private void OnValue1TextChange(string value)
	{
		_value1 = value;
		if (_filterType == FilterTypes.In)
		{
			_selectedValues.Clear();
			_selectedValues.AddRange([.. _value1.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())]);
		}
	}

	private void OnValue2TextChange(string value) => _value2 = value;

	private async Task OnValuesFilterTextChange(string value)
	{
		_valuesFilter = value;
		await RefreshValues().ConfigureAwait(true);
	}

	private void OnFilterTypeBindAfter()
	{
		if (_filterType == FilterTypes.Range)
		{
			// ranges should be in order
			_selectedValues.Sort();
		}

		// store the temp values
		var tempValue1 = _selectedValues.ElementAtOrDefault(0);
		var tempValue2 = _selectedValues.ElementAtOrDefault(1);

		// if single selection and compatible operator - simple copy value
		if (tempValue1 != null && _singleValueTypes.Contains(_filterType))
		{
			_selectedValues.Clear();
			_selectedValues.Add(tempValue1);
			_value1 = tempValue1;
		}

		if (tempValue2 != null && _filterType == FilterTypes.Range)
		{
			_selectedValues.Add(tempValue2);
			_value2 = tempValue2;
		}

		if (_filterType is FilterTypes.NotIn or FilterTypes.In)
		{
			_value1 = JoinSelectedValues();
			_value2 = string.Empty;
		}
	}

	private void OnValueClicked(string value)
	{
		// if single value then clear other selections
		if (!Options.AllowIn)
		{
			_selectedValues.Clear();
			_selectedValues.Add(value);
			_value1 = value;
			return;
		}

		ToggleSelectedValue(value);
		UpdateValuesFromSelection();
	}

	private void ToggleSelectedValue(string value)
	{
		// toggle clicked value from selected items
		if (_selectedValues.Remove(value))
		{
			return;
		}

		// Clear existing if not auto change to multi
		if (_singleOnlyTypes.Contains(_filterType))
		{
			_selectedValues.Clear();
		}

		_selectedValues.Add(value);
	}

	private void UpdateValuesFromSelection()
	{
		// if single selection and compatible operator - simple copy value
		if (_selectedValues.Count == 1 && _singleValueTypes.Contains(_filterType))
		{
			_value1 = _selectedValues[0];
			_value2 = string.Empty;
		}
		else if (_selectedValues.Count == 2 && _filterType == FilterTypes.Range)
		{
			_selectedValues.Sort();
			_value1 = _selectedValues[0];
			_value2 = _selectedValues[1];
		}
		else if (_selectedValues.Count == 0)
		{
			// do nothing if unselected the last one
			_value1 = string.Empty;
		}
		else
		{
			if (_filterType != FilterTypes.NotIn)
			{
				_filterType = _filterType == FilterTypes.DoesNotEqual ? FilterTypes.NotIn : FilterTypes.In;
			}

			_value1 = JoinSelectedValues();
		}
	}

	private string JoinSelectedValues() => string.Join("|", _selectedValues.Select(x => x.QuoteIfContainsWhitespace()));

	private async Task RefreshValues()
	{
		if (ShowValues && FetchValuesAsync != null)
		{
			var filter = new Filter
			{
				FilterType = FilterTypes.Contains,
				Value = _valuesFilter,
				Key = Filter.Key
			};
			_values = await FetchValuesAsync.Invoke(filter).ConfigureAwait(true);
		}
	}
}
