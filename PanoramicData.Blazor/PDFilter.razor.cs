﻿namespace PanoramicData.Blazor;

public partial class PDFilter : IAsyncDisposable
{
	private static int _sequence;
	private readonly string _id = $"filter-button-{(++_sequence)}";
	private PDDropDown _dropDown = null!;
	private string[] _values = [];
	private string _value1 = string.Empty;
	private string _value2 = string.Empty;
	private FilterTypes _filterType = FilterTypes.Equals;
	private string _valuesFilter = string.Empty;
	private readonly List<string> _selectedValues = [];
	private IJSObjectReference? _commonModule;
	private static readonly char[] _separator = ['|'];

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter]
	public string CssClass { get; set; } = "p-0 ms-1";

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
	public bool Nullable { get; set; }

	[Parameter]
	public FilterOptions Options { get; set; } = new();

	[Parameter]
	public bool ShowValues { get; set; } = true;

	[Parameter]
	public ButtonSizes Size { get; set; } = ButtonSizes.Small;


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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_commonModule = await JSRuntime
					.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js")
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
		await _dropDown.HideAsync().ConfigureAwait(true);
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
			_selectedValues.AddRange(_value1.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes()).ToArray());
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
		await _dropDown.HideAsync().ConfigureAwait(true);
		await FilterChanged.InvokeAsync(Filter).ConfigureAwait(true);
	}

	private void OnValue1TextChange(string value)
	{
		_value1 = value;
		if (_filterType == FilterTypes.In)
		{
			_selectedValues.Clear();
			_selectedValues.AddRange(_value1.Split(_separator, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes()).ToArray());
		}
		else if (_filterType == FilterTypes.Equals)
		{
			_value1 = _value1.QuoteIfContainsWhitespace();
		}
	}

	private void OnValue2TextChange(string value) => _value2 = value;

	private async Task OnValuesFilterTextChange(string value)
	{
		_valuesFilter = value;
		await RefreshValues().ConfigureAwait(true);
	}

	private void OnFilterTypeChanged(ChangeEventArgs args) => _filterType = (FilterTypes)Enum.Parse(typeof(FilterTypes), args.Value?.ToString() ?? string.Empty);

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

		// toggle clicked value from selected items
		if (!_selectedValues.Remove(value))
		{
			_selectedValues.Add(value);
		}

		// if single selection and compatible operator - simple copy value
		var ops = new[] { FilterTypes.Equals, FilterTypes.DoesNotEqual, FilterTypes.GreaterThan, FilterTypes.GreaterThanOrEqual, FilterTypes.LessThan, FilterTypes.LessThanOrEqual, FilterTypes.Range };
		if (_selectedValues.Count == 1 && ops.Contains(_filterType))
		{
			_value1 = _selectedValues[0].QuoteIfContainsWhitespace();
		}
		else if (_selectedValues.Count == 2 && _filterType == FilterTypes.Range)
		{
			_value1 = _selectedValues[0].QuoteIfContainsWhitespace();
			_value2 = _selectedValues[1].QuoteIfContainsWhitespace();
		}
		else
		{
			if (_filterType != FilterTypes.NotIn)
			{
				_filterType = FilterTypes.In;
			}

			_value1 = string.Join("|", _selectedValues.Select(x => x.QuoteIfContainsWhitespace()).ToArray());
		}

	}

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
