namespace PanoramicData.Blazor;

public partial class PDTableColumnSelector<TItem> where TItem : class
{
	private readonly List<IDisplayItem> _columns = new();

	[Parameter]
	[EditorRequired]
	public PDTable<TItem>? Table { get; set; }

	[Parameter]
	public bool CanChangeOrder { get; set; } = true;

	[Parameter]
	public bool CanChangeVisible { get; set; } = true;

	public void OnOrderChanged(DragOrderChangeArgs<IDisplayItem> args)
	{
		// commit ordering change
		_columns.Clear();
		_columns.AddRange(args.Items);

		// update column ordinals
		if (Table != null)
		{
			var newOrder = args.Items.ToList();
			foreach (var column in Table.Columns.Where(x => x.ShowInList))
			{
				var newOrdinal = newOrder.FindIndex(x => x.Id == column.Id);
				if (newOrdinal < 0)
				{
					newOrdinal = 1000;
				}
				if (column.Ordinal != newOrdinal)
				{
					column.SetOrdinal(newOrdinal);
				}
			}
			// at least 1 change made else callback event would not fire
			Table.SetStateHasChanged();
		}
	}

	protected override void OnParametersSet()
	{
		if (Table != null && _columns.Count == 0)
		{
			// initialize to all shown columns
			foreach (var column in Table.Columns.Where(x => x.ShowInList))
			{
				var text = string.IsNullOrWhiteSpace(column.Name) ? column.GetTitle() ?? string.Empty : column.Name;
				if (CanChangeVisible)
				{
					_columns.Add(new SelectableItem(column.Id, text)
					{
						IsSelected = column.IsVisible,
						IsEnabled = CanChangeVisible && column.CanToggleVisible
					});
				}
				else
				{
					_columns.Add(new BasicItem(column.Id, text));
				}
			}
		}
	}

	public void OnSelectionChanged(IEnumerable<IDisplayItem> selection)
	{
		// update column visibilities
		if (Table != null)
		{
			int changes = 0;
			foreach (var column in Table.Columns.Where(x => x.CanToggleVisible))
			{
				var isVisible = selection.Any(x => x.Id == column.Id);
				if (isVisible != column.State.Visible)
				{
					column.SetVisible(isVisible);
					changes++;
				}
			}
			if (changes > 0)
			{
				Table.SetStateHasChanged();
			}
		}

	}
}