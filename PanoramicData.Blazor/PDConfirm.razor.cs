namespace PanoramicData.Blazor;

public partial class PDConfirm : PDModalBase
{
	private ToolbarButton _noButton = new() { Key = ModalResults.NO, Text = "No" };
	private ToolbarButton _yesButton = new() { Key = ModalResults.YES, Text = "Yes", CssClass = "btn-primary", ShiftRight = true };

	public PDConfirm()
	{
		// override modal standard defaults
		CloseOnEscape = false;
		HideOnBackgroundClick = false;
		ShowClose = false;
		Title = "Confirm Action";
		Buttons = new List<ToolbarItem>
		{
			_yesButton,
			_noButton
		};
	}

	/// <summary>
	/// Sets the content displayed in the modal dialog body.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets the message to be displayed if the ChildContent not suupplied.
	/// </summary>
	[Parameter]
	public string Message { get; set; } = "Are you sure?";

	/// <summary>
	/// Gets the text displayed on the No button.
	/// </summary>
	[Parameter]
	public string NoText { get; set; } = "No";

	/// <summary>
	/// Gets the text displayed on the Yes button.
	/// </summary>
	[Parameter]
	public string YesText { get; set; } = "Yes";

	protected override void OnParametersSet()
	{
		// update Yes and No text?
		_yesButton.Text = YesText;
		_noButton.Text = NoText;
	}

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync(string message) => ShowAndWaitResultAsync(message, CancellationToken.None);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync(string message, CancellationToken cancellationToken)
	{
		Message = message;
		StateHasChanged();
		return Modal.ShowAndWaitResultAsync(cancellationToken);
	}

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync(string message, string title) => ShowAndWaitResultAsync(message, title, default);

	/// <summary>
	/// Displays the Modal Dialog and awaits the users choice.
	/// </summary>
	public Task<string> ShowAndWaitResultAsync(string message, string title, CancellationToken cancellationToken)
	{
		Message = message;
		Title = title;
		StateHasChanged();
		return Modal.ShowAndWaitResultAsync(cancellationToken);
	}
}
