namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDListPage
{
	private PDDropDown? _dropdown;
	private bool _isEnabled = true;
	private CarDataProvider _dataProvider = new();

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
}
