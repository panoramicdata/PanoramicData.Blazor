namespace PanoramicData.Blazor;

public partial class PDCardDeck<TItem>
{
	private static int _sequence;
	private readonly List<TItem> _selection = new();

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter]
	public string Id { get; set; } = $"pd-toggleswitch-{++_sequence}";

	[EditorRequired]
	[Parameter]
	public RenderFragment<TItem>? CardTemplate { get; set; }

	[Parameter]
	public IEnumerable<TItem> Items { get; set; } = Array.Empty<TItem>();

	[Parameter]
	public bool MultipleSelection { get; set; }

	public IEnumerable<TItem> GetSelection() => _selection;

	private Task OnItemMouseUpAsync(TItem item, MouseEventArgs args)
	{
		UpdateSelection(item, args.CtrlKey, args.ShiftKey);
		return Task.CompletedTask;
	}

	private void UpdateSelection(TItem item, bool ctrl, bool shift)
	{
		if (IsEnabled)
		{
			if (MultipleSelection)
			{
				if (ctrl) // add/remove single
				{
					if (_selection.Contains(item))
					{
						_selection.Remove(item);
					}
					else
					{
						_selection.Add(item);
					}
				}
				else if (shift && _selection.Count > 0) // add range
				{
					var list = Items.ToList();
					var sIdx = list.IndexOf(_selection.First());
					var eIdx = list.IndexOf(item);
					var s = Math.Min(sIdx, eIdx);
					var e = Math.Max(sIdx, eIdx);
					_selection.Clear();
					_selection.AddRange(list.GetRange(s, e - s + 1));
				}
				else
				{
					_selection.Clear();
					_selection.Add(item);
				}
			}
			else
			{
				_selection.Clear();
				_selection.Add(item);
			}
		}
	}

	private string SizeCssClass => Size switch
	{
		ButtonSizes.Small => "sm",
		ButtonSizes.Large => "lg",
		_ => "md"
	};
}
