namespace PanoramicData.Blazor;

public partial class PDConfirm : PDModalBase
{
	private readonly ToolbarButton _cancelButton = new() { Key = ModalResults.CANCEL, Text = "Cancel", IsVisible = false };
	private readonly ToolbarButton _noButton = new() { Key = ModalResults.NO, Text = "No" };
	private readonly ToolbarButton _yesButton = new() { Key = ModalResults.YES, Text = "Yes", CssClass = "btn-primary", ShiftRight = true };

	public PDConfirm()
	{
		// override modal standard defaults
		CloseOnEscape = false;
		HideOnBackgroundClick = false;
		ShowClose = false;
		Title = "Confirm Action";
		Buttons =
		[
			_yesButton,
			_noButton,
			_cancelButton
		];
	}

	/// <summary>
	/// Sets the button size in the modal footer.
	/// </summary>
	[Parameter]
	public ButtonSizes ButtonSize { get; set; } = ButtonSizes.Medium;

	/// <summary>
	/// Gets the text displayed on the Cancel button.
	/// </summary>
	[Parameter]
	public string CancelText { get; set; } = "Cancel";

	/// <summary>
	/// Sets the content displayed in the modal dialog body.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets the message to be displayed if the ChildContent not supplied.
	/// </summary>
	[Parameter]
	public string Message { get; set; } = "Are you sure?";

	/// <summary>
	/// Gets the text displayed on the No button.
	/// </summary>
	[Parameter]
	public string NoText { get; set; } = "No";

	/// <summary>
	/// Gets whether to show the Cancel button?
	/// </summary>
	[Parameter]
	public bool ShowCancel { get; set; }

	/// <summary>
	/// Gets the text displayed on the Yes button.
	/// </summary>
	[Parameter]
	public string YesText { get; set; } = "Yes";

	protected override void OnParametersSet()
	{
		// update Yes, No and Cancel text?
		_cancelButton.Text = CancelText;
		_cancelButton.IsVisible = ShowCancel;
		_noButton.Text = NoText;
		_yesButton.Text = YesText;
	}

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public new async Task<Outcomes> ShowAndWaitResultAsync() => await Modal.ShowAndWaitResultAsync() switch
	{
		ModalResults.YES => Outcomes.Yes,
		ModalResults.NO => Outcomes.No,
		_ => Outcomes.Cancel
	};

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<Outcomes> ShowAndWaitResultAsync(string message) => ShowAndWaitResultAsync(message, CancellationToken.None);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public async Task<Outcomes> ShowAndWaitResultAsync(string message, CancellationToken cancellationToken)
	{
		Message = message;
		StateHasChanged();
		return await Modal.ShowAndWaitResultAsync(cancellationToken) switch
		{
			ModalResults.YES => Outcomes.Yes,
			ModalResults.NO => Outcomes.No,
			_ => Outcomes.Cancel
		};
	}

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<Outcomes> ShowAndWaitResultAsync(string message, string title) => ShowAndWaitResultAsync(message, title, default);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public new async Task<Outcomes> ShowAndWaitResultAsync(CancellationToken cancellationToken)
	{
		return await Modal.ShowAndWaitResultAsync(cancellationToken) switch
		{
			ModalResults.YES => Outcomes.Yes,
			ModalResults.NO => Outcomes.No,
			_ => Outcomes.Cancel
		};
	}

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public async Task<Outcomes> ShowAndWaitResultAsync(string message, string title, CancellationToken cancellationToken)
	{
		Message = message;
		Title = title;
		StateHasChanged();
		return await Modal.ShowAndWaitResultAsync(cancellationToken) switch
		{
			ModalResults.YES => Outcomes.Yes,
			ModalResults.NO => Outcomes.No,
			_ => Outcomes.Cancel
		};
	}

	/// <summary>
	/// Enumeration of possible Confirm dialog outcomes.
	/// </summary>
	public enum Outcomes
	{
		Yes,
		No,
		Cancel
	}
}
