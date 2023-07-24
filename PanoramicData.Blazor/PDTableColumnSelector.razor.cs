namespace PanoramicData.Blazor;

public partial class PDTableColumnSelector<TItem> where TItem : class
{
	private readonly List<IDisplayItem> _items = new();

	[Parameter]
	[EditorRequired]
	public PDTable<TItem>? Table { get; set; }

	[Parameter]
	public bool CanChangeOrder { get; set; } = true;

	[Parameter]
	public bool CanChangeVisible { get; set; } = true;

	public async Task OnOrderChanged(DragOrderChangeArgs<IDisplayItem> args)
	{
		// commit ordering change
		_items.Clear();
		_items.AddRange(args.Items);

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
			await Table.SaveStateAsync();
			Table.SetStateHasChanged();
		}
	}

	protected override void OnParametersSet()
	{
		if (Table != null)
		{
			// initialize to all shown columns
			_items.Clear();
			foreach (var column in Table.Columns.Where(x => x.ShowInList).OrderBy(x => x.State.Ordinal))
			{
				var text = string.IsNullOrWhiteSpace(column.Name) ? column.GetTitle() ?? string.Empty : column.Name;
				if (CanChangeVisible)
				{
					_items.Add(new SelectableItem
					{
						Id = column.Id,
						IsSelected = column.State.Visible,
						IsEnabled = CanChangeVisible && column.CanToggleVisible,
						Text = text
					});
				}
				else
				{
					_items.Add(new BasicItem
					{
						Id = column.Id,
						Text = text
					});
				}
			}
		}
	}

	public async Task OnSelectionChanged(IEnumerable<IDisplayItem> selection)
	{
		// update column visibilities
		if (Table != null)
		{
			foreach (var column in Table.Columns.Where(x => x.CanToggleVisible))
			{
				var isVisible = selection.Any(x => x.Id == column.Id);
				column.SetVisible(isVisible);
			}
			await Table.SaveStateAsync();
			Table.SetStateHasChanged();
		}
	}
}