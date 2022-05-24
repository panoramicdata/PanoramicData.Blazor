namespace PanoramicData.Blazor;

public partial class PDFilter
{
	private static int _sequence = 0;
	private string _id = $"filter-button-{(++_sequence)}";
	private PDDropDown _dropDown = null!;
	private string[] _values = Array.Empty<string>();
	private string _value1 = String.Empty;
	private string _value2 = String.Empty;
	private FilterTypes _filterType = FilterTypes.Equals;
	private string _valuesFilter = String.Empty;
	private List<string> _selectedValues = new List<string>();

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter]
	public Filter Filter { get; set; } = new Filter();

	[Parameter]
	public EventCallback<Filter> FilterChanged { get; set; }

	[Parameter]
	public Func<Filter, Task<string[]>>? FetchValuesAsync { get; set; }

	[Parameter]
	public string IconCssClass { get; set; } = "fas fa-filter";

	[Parameter]
	public FilterDataTypes DataType { get; set; }

	[Parameter]
	public bool ShowValues { get; set; } = true;

	[Parameter]
	public ButtonSizes Size { get; set; } = ButtonSizes.Small;

	private string CssClass => $"p-0 pd-filter {(HasFilter ? "filtered" : "")}";

	private bool HasFilter => !string.IsNullOrWhiteSpace(Filter.Value);

	private async Task OnClear()
	{
		_filterType = FilterTypes.Equals;
		_value1 = String.Empty;
		_value2 = String.Empty;
		_selectedValues.Clear();
		Filter.Clear();
		await FilterChanged.InvokeAsync(Filter).ConfigureAwait(true);
	}

	private async Task OnDropDownShown()
	{
		_selectedValues.Clear();
		_filterType = Filter.FilterType;
		_value1 = Filter.Value;
		_value2 = Filter.Value2;
		await RefreshValues().ConfigureAwait(true);
		if (_filterType == FilterTypes.In)
		{
			_selectedValues.AddRange(_value1.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes()).ToArray());
		}
	}

	private async Task OnDropDownKeyPress(int keyCode)
	{
		if (keyCode == 13)
		{
			// can happen before lost focus and hence text value not updated
			// so force focus to filter button and perform click
			await JSRuntime.InvokeVoidAsync("panoramicData.focus", _id).ConfigureAwait(true);
			await JSRuntime.InvokeVoidAsync("panoramicData.click", _id).ConfigureAwait(true);
		}
	}

	private async Task OnFilter()
	{
		Filter.FilterType = _filterType;
		Filter.Value = _value1;
		Filter.Value2 = _value2;
		await FilterChanged.InvokeAsync(Filter).ConfigureAwait(true);
	}

	private void OnValue1TextChange(string value)
	{
		_value1 = value;
		if (_filterType == FilterTypes.In)
		{
			_selectedValues.Clear();
			_selectedValues.AddRange(_value1.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes()).ToArray());
		}
	}

	private void OnValue2TextChange(string value)
	{
		_value2 = value;
	}

	private async Task OnValuesFilterTextChange(string value)
	{
		_valuesFilter = value;
		await RefreshValues().ConfigureAwait(true);
	}

	private void OnFilterTypeChanged(ChangeEventArgs args)
	{
		_filterType = (FilterTypes)Enum.Parse(typeof(FilterTypes), args.Value?.ToString() ?? String.Empty);
	}

	private void OnValueClicked(string value)
	{
		if (_selectedValues.Contains(value))
		{
			_selectedValues.Remove(value);
		}
		else
		{
			_selectedValues.Add(value);
		}
		_filterType = FilterTypes.In;
		_value1 = String.Join("|", _selectedValues.Select(x => x.QuoteIfContainsWhitespace()).ToArray());
	}

	private async Task RefreshValues()
	{
		if (FetchValuesAsync != null)
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
