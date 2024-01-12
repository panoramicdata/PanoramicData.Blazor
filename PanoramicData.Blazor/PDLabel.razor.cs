namespace PanoramicData.Blazor;

public partial class PDLabel
{
	[Parameter]
	public EventCallback<MouseEventArgs> Click { get; set; }

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	[Parameter]
	public object? DataItem { get; set; }

	[Parameter]
	public string IconCssClass { get; set; } = string.Empty;

	[Parameter]
	public EventCallback<MouseEventArgs> MouseDown { get; set; }

	[Parameter]
	public bool PreventDefault { get; set; }

	[Parameter]
	public EventCallback<ISelectable> SelectedChanged { get; set; }

	[Parameter]
	public bool StopPropagation { get; set; }

	[Parameter]
	public string Text { get; set; } = string.Empty;

	[Parameter]
	public string TextCssClass { get; set; } = string.Empty;

	[Parameter]
	public string ToolTip { get; set; } = string.Empty;

	private static IDictionary<string, object> GetCheckboxAttributes(ISelectable item)
	{
		var dict = new Dictionary<string, object>
		{
			{ "class", "me-1" },
			{ "type", "checkbox" }
		};
		if (item.IsSelected)
		{
			dict.Add("checked", true);
		}

		if (!item.IsEnabled)
		{
			dict.Add("disabled", true);
		}

		return dict;
	}

	private async Task OnSelectedChanged(ChangeEventArgs args)
	{
		if (DataItem is ISelectable si)
		{
			var newValue = Convert.ToBoolean(args.Value, CultureInfo.InvariantCulture);
			if (newValue != si.IsSelected)
			{
				si.IsSelected = newValue;
				await SelectedChanged.InvokeAsync(si);
			}
		}
	}
}
