namespace PanoramicData.Blazor;

/// <summary>
/// A container component that manages the layout and positioning of chat functionality
/// alongside other content. Automatically handles scrolling and overflow behavior
/// without requiring additional CSS from the consuming application.
/// </summary>
/// <remarks>
/// The PDChatContainer uses isolated CSS to automatically provide proper scrolling
/// behavior for content placed within it. This eliminates the need for consumers
/// to add custom CSS for layout and overflow handling.
/// </remarks>
public partial class PDChatContainer : ComponentBase
{
	[Parameter] public RenderFragment? ChildContent { get; set; }
	[Parameter] public RenderFragment? ChatContent { get; set; }

	/// <summary>
	/// Initial dock mode for the chat. If not specified, defaults to Minimized.
	/// The container will automatically manage dock mode changes internally.
	/// </summary>
	[Parameter] public PDChatDockMode InitialDockMode { get; set; } = PDChatDockMode.Minimized;

	/// <summary>
	/// Callback fired when the dock mode changes. Optional - for external monitoring only.
	/// </summary>
	[Parameter] public EventCallback<PDChatDockMode> DockModeChanged { get; set; }

	[Parameter] public int GutterSize { get; set; } = 6;
	[Parameter] public int ChatPanelSize { get; set; } = 2;
	[Parameter] public int TotalSize { get; set; } = 5;
	[Parameter] public int ChatMinSize { get; set; } = 200;
	[Parameter] public int ContentMinSize { get; set; } = 200;

	[Parameter] public required IChatService ChatService { get; set; }

	/// <summary>
	/// Gets whether the container is currently in split mode.
	/// Used by child components to adjust their behaviour.
	/// </summary>
	internal bool IsSplitMode => ChatService.DockMode is PDChatDockMode.Left or PDChatDockMode.Right;

	/// <summary>
	/// Internal method called by PDChat when dock mode changes.
	/// This keeps the synchronization internal to the container.
	/// </summary>
	internal async Task OnInternalDockModeChanged(PDChatDockMode newDockMode)
	{
		ChatService.DockMode = newDockMode;

		StateHasChanged();

		// Optionally notify external listeners
		if (DockModeChanged.HasDelegate)
		{
			await DockModeChanged.InvokeAsync(newDockMode);
		}
	}
}