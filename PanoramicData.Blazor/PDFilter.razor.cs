﻿namespace PanoramicData.Blazor;

public partial class PDFilter : IAsyncDisposable
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
	private IJSObjectReference? _commonModule;

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

	protected override async Task OnInitializedAsync()
	{
		_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js").ConfigureAwait(true);
	}

	private async Task OnClear()
	{
		_filterType = FilterTypes.Equals;
		_value1 = String.Empty;
		_value2 = String.Empty;
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
			_selectedValues.AddRange(_value1.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes()).ToArray());
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
