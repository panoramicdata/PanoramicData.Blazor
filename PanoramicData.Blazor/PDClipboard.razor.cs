namespace PanoramicData.Blazor;

public partial class PDClipboard
{
	[Inject] IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// The text to show next to (on the right) of the copy button. If not set, only the button will be shown
	/// </summary>
	[Parameter] public string ButtonText { get; set; } = string.Empty;

	/// <summary>
	/// The CSS class to apply to the text next to the copy button. If not set, no CSS class will be applied
	/// </summary>
	[Parameter] public string ButtonTextCssClass { get; set; } = string.Empty;

	/// <summary>
	/// General CSS Class to apply
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// CSS class to apply when the copy button is ready to be clicked
	/// </summary>
	[Parameter] public string ReadyToCopyCssClass { get; set; } = "far fa-copy";

	/// <summary>
	/// Text to be copied.
	/// </summary>
	[Parameter] public string Text { get; set; } = string.Empty;

	/// <summary>
	/// CSS class to apply when the text has been copied
	/// </summary>
	[Parameter] public string TextCopiedCssClass { get; set; } = "fas fa-check";

	/// <summary>
	/// Text displayed as a tooltip.
	/// </summary>
	[Parameter] public string ToolTip { get; set; } = "Copy to clipboard";

	private string _buttonClass = string.Empty;

	protected override void OnInitialized() => _buttonClass = ReadyToCopyCssClass;

	private async Task CopyTextToClipboardAsync()
	{
		await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", Text);
		_buttonClass = TextCopiedCssClass;
		StateHasChanged();
		await Task.Delay(1000);
		_buttonClass = ReadyToCopyCssClass;
		StateHasChanged();
	}
}
