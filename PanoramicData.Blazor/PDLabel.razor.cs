namespace PanoramicData.Blazor;

public partial class PDLabel
{
	/// <summary>
	/// An event callback that is invoked when the label is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> Click { get; set; }

	/// <summary>
	/// Gets or sets the child content of the label.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the label.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the data item associated with the label.
	/// </summary>
	[Parameter]
	public object? DataItem { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the icon.
	/// </summary>
	[Parameter]
	public string IconCssClass { get; set; } = string.Empty;

	/// <summary>
	/// An event callback that is invoked when the mouse button is pressed down on the label.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> MouseDown { get; set; }

	/// <summary>
	/// An event callback that is invoked when the mouse pointer enters the label.
	/// </summary>
	[Parameter]
	public EventCallback<MouseEventArgs> MouseEnter { get; set; }

	/// <summary>
	/// Gets or sets whether to prevent the default action of the event.
	/// </summary>
	[Parameter]
	public bool PreventDefault { get; set; }

	/// <summary>
	/// An event callback that is invoked when the selection state of the data item changes.
	/// </summary>
	[Parameter]
	public EventCallback<ISelectable> SelectedChanged { get; set; }

	/// <summary>
	/// Gets or sets whether to stop the event from propagating further.
	/// </summary>
	[Parameter]
	public bool StopPropagation { get; set; }

	/// <summary>
	/// Gets or sets the text to be displayed on the label.
	/// </summary>
	[Parameter]
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the CSS class for the text.
	/// </summary>
	[Parameter]
	public string TextCssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the tooltip for the label.
	/// </summary>
	[Parameter]
	public string ToolTip { get; set; } = string.Empty;

	private static Dictionary<string, object> GetCheckboxAttributes(ISelectable item)
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
