using System.Diagnostics.CodeAnalysis;

namespace PanoramicData.Blazor;

/// <summary>
/// Base class for modal wrappers that expose common configuration and show/hide helpers.
/// </summary>
public abstract class PDModalBase : ComponentBase
{
	/// <summary>
	/// Gets or sets the inner modal component instance.
	/// </summary>
	[AllowNull]
	protected PDModal Modal { get; set; }

	/// <summary>
	/// Gets or sets the footer buttons displayed by the modal.
	/// </summary>
	[Parameter]
	public List<ToolbarItem> Buttons { get; set; } =
	[
		new ToolbarButton { Key = ModalResults.YES, Text = "Yes", CssClass = "btn-primary", ShiftRight = true },
		new ToolbarButton { Key = ModalResults.NO, Text = "No" },
		new ToolbarButton { Key = ModalResults.CANCEL, Text = "Cancel" }
	];

	/// <summary>
	/// Gets or sets whether the modal is vertically centered.
	/// </summary>
	[Parameter]
	public bool CenterVertically { get; set; }

	/// <summary>
	/// Gets or sets whether pressing Escape closes the modal.
	/// </summary>
	[Parameter]
	public bool CloseOnEscape { get; set; } = true;

	/// <summary>
	/// Gets or sets whether clicking the backdrop hides the modal.
	/// </summary>
	[Parameter]
	public bool HideOnBackgroundClick { get; set; }

	/// <summary>
	/// Gets or sets the callback invoked when the modal is hidden.
	/// </summary>
	[Parameter]
	public EventCallback<string> ModalHidden { get; set; }

	/// <summary>
	/// Gets or sets whether the close button is shown in the header.
	/// </summary>
	[Parameter]
	public bool ShowClose { get; set; } = true;

	/// <summary>
	/// Gets or sets the modal size.
	/// </summary>
	[Parameter]
	public ModalSizes Size { get; set; } = ModalSizes.Large;

	/// <summary>
	/// Gets or sets the modal title.
	/// </summary>
	[Parameter]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	/// Hides the Modal Dialog.
	/// </summary>
	public Task HideAsync() => HideAsync(default);

	/// <summary>
	/// Hides the Modal Dialog.
	/// </summary>
	/// <param name="cancellationToken">Token used to cancel the hide operation.</param>
	public Task HideAsync(CancellationToken cancellationToken) => Modal.HideAsync(cancellationToken);

	/// <summary>
	/// Displays the Modal Dialog.
	/// </summary>
	public Task ShowAsync() => Modal.ShowAsync(default);

	/// <summary>
	/// Displays the Modal Dialog.
	/// </summary>
	/// <param name="cancellationToken">Token used to cancel the show operation.</param>
	public Task ShowAsync(CancellationToken cancellationToken) => Modal.ShowAsync(cancellationToken);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync() => ShowAndWaitResultAsync(default);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	/// <param name="cancellationToken">Token used to cancel the await operation.</param>
	public Task<string> ShowAndWaitResultAsync(CancellationToken cancellationToken) => Modal.ShowAndWaitResultAsync(cancellationToken);
}
