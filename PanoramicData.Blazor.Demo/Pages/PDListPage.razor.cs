namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDListPage
{
	private CarDataProvider _dataProvider = new();

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	private void OnSelectionChanged(SelectionArgs<Car> args)
	{
		EventManager?.Add(new Event("SelectionChanged", new EventArgument("All", args.Selection.AllSelected), new EventArgument("Items", string.Join(", ", args.Selection.Items))));
	}
}
