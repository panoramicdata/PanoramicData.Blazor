using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor;

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

    // Internal dock mode management - no longer exposed as a parameter
    private PDChatDockMode _currentDockMode;

    /// <summary>
    /// Gets whether the container is currently in split mode.
    /// Used by child components to adjust their behavior.
    /// </summary>
    internal bool IsSplitMode => _currentDockMode is PDChatDockMode.Left or PDChatDockMode.Right;

    protected override void OnInitialized()
    {
        // Initialize internal dock mode from the initial parameter
        _currentDockMode = InitialDockMode;
        base.OnInitialized();
    }

    /// <summary>
    /// Internal method called by PDChat when dock mode changes.
    /// This keeps the synchronization internal to the container.
    /// </summary>
    internal async Task OnInternalDockModeChanged(PDChatDockMode newDockMode)
    {
        if (_currentDockMode != newDockMode)
        {
            _currentDockMode = newDockMode;
            StateHasChanged();
            
            // Optionally notify external listeners
            if (DockModeChanged.HasDelegate)
            {
                await DockModeChanged.InvokeAsync(newDockMode);
            }
        }
    }

    /// <summary>
    /// Gets the current dock mode for child components.
    /// This ensures PDChat uses the container's dock mode.
    /// </summary>
    internal PDChatDockMode GetCurrentDockMode() => _currentDockMode;

    private SplitDirection GetSplitDirection()
    {
        return _currentDockMode switch
        {
            PDChatDockMode.Left or PDChatDockMode.Right => SplitDirection.Horizontal,
            _ => SplitDirection.Horizontal
        };
    }

    private bool IsChatFirstPanel()
    {
        return _currentDockMode switch
        {
            PDChatDockMode.Left => true,
            PDChatDockMode.Right => false,
            _ => false
        };
    }
}