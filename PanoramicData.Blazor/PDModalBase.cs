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
	public Task HideAsync() => HideAsync(default);

	/// <summary>
	/// Hides the Modal Dialog.
	/// </summary>
	public Task HideAsync(CancellationToken cancellationToken) => Modal.HideAsync(cancellationToken);

	/// <summary>
	/// Displays the Modal Dialog.
	/// </summary>
	public Task ShowAsync() => Modal.ShowAsync(default);

	/// <summary>
	/// Displays the Modal Dialog.
	/// </summary>
	public Task ShowAsync(CancellationToken cancellationToken) => Modal.ShowAsync(cancellationToken);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync() => ShowAndWaitResultAsync(default);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync(CancellationToken cancellationToken) => Modal.ShowAndWaitResultAsync(cancellationToken);
}
