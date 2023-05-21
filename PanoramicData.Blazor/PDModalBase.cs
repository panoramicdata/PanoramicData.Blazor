using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor;

public abstract class PDModalBase : ComponentBase
{
	[AllowNull]
	protected PDModal Modal { get; set; }

	[Parameter]
	public List<ToolbarItem> Buttons { get; set; } = new List<ToolbarItem>
	{
		new ToolbarButton { Key = ModalResults.YES, Text = "Yes", CssClass = "btn-primary", ShiftRight = true },
		new ToolbarButton { Key = ModalResults.NO, Text = "No" },
		new ToolbarButton { Key = ModalResults.CANCEL, Text = "Cancel" }
	};

	[Parameter]
	public bool CenterVertically { get; set; }

	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	[Parameter]
	public bool HideOnBackgroundClick { get; set; }

	[Parameter]
	public EventCallback<string> ModalHidden { get; set; }

	[Parameter]
	public bool ShowClose { get; set; } = true;

	[Parameter]
	public ModalSizes Size { get; set; } = ModalSizes.Large;

	[Parameter]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Hides the Modal Dialog.
	/// </summary>
	public Task HideAsync() => Modal.HideAsync();

	/// <summary>
	/// Displays the Modal Dialog.
	/// </summary>
	public Task ShowAsync() => Modal.ShowAsync();

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync() => Modal.ShowAndWaitResultAsync();
}
