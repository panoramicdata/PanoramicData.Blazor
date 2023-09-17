using System.Linq.Expressions;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDListPage
{
	private PDDropDown? _dropdown;
	private bool _isEnabled = true;
	private CarDataProvider _dataProvider = new();
	private Expression<Func<Car, object>> _sortExpression = (x) => x == null || x.ToString() == null ? string.Empty : x.ToString()!;
	private Selection<Car> _list5Selection = new();

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }


	private void OnApply(Selection<Car> selection)
	{
		EventManager?.Add(new Event("Apply", new EventArgument("All", selection.AllSelected), new EventArgument("Items", string.Join(", ", selection.Items))));
	}

	private void OnCancel()
	{
		EventManager?.Add(new Event("Cancel"));
	}

	private Task OnDropDownApplyAsync(Selection<Car> selection)
	{
		EventManager?.Add(new Event("Apply", new EventArgument("All", selection.AllSelected), new EventArgument("Items", string.Join(", ", selection.Items))));
		return _dropdown!.HideAsync();
	}

	private Task OnDropDownCancelAsync()
	{
		EventManager?.Add(new Event("Cancel"));
		return _dropdown!.HideAsync();
	}

	private void OnSelectionChanged(Selection<Car> selection)
	{
		EventManager?.Add(new Event("SelectionChanged", new EventArgument("All", selection.AllSelected), new EventArgument("Items", string.Join(", ", selection.Items))));
	}

	protected override async Task OnInitializedAsync()
	{
		// set up initial selection for list 5 (all BMW abd Fords)
		var request = new DataRequest<Car>();
		var response = await _dataProvider.GetDataAsync(request, default);
		_list5Selection.Items.AddRange(response.Items.Where(x => x.Make == "BMW" || x.Make == "Ford"));
	}
}
