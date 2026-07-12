namespace PanoramicData.Blazor;

/// <summary>
/// A collapsible section component with an animated chevron, accessible markup,
/// and fully overridable CSS custom properties for theming.
/// </summary>
public partial class PDSection : ComponentBase
{
    private static int _idCounter;

    /// <summary>
    /// Gets or sets additional CSS classes applied to the outer container element.
    /// </summary>
    [Parameter]
    public string CssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional CSS classes applied to the header button element.
    /// </summary>
    [Parameter]
    public string HeaderCssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional CSS classes applied to the body wrapper element.
    /// </summary>
    [Parameter]
    public string BodyCssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional CSS classes applied to the title element.
    /// </summary>
    [Parameter]
    public string TitleCssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional CSS classes applied to the secondary title element.
    /// </summary>
    [Parameter]
    public string SecondaryTitleCssClass { get; set; } = "text-muted ms-2 fw-normal";

    /// <summary>
    /// Gets or sets the primary title text. Ignored when <see cref="TitleTemplate"/> is set.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional secondary title text rendered beside the primary title.
    /// Ignored when <see cref="TitleTemplate"/> is set.
    /// </summary>
    [Parameter]
    public string SecondaryTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the heading level (1-6) used to render the title as an H element.
    /// When null (default) the title is rendered as a plain span.
    /// </summary>
    [Parameter]
    public int? HeadingLevel { get; set; }

    /// <summary>
    /// Gets or sets a custom render fragment for the title area. When set, <see cref="Title"/>,
    /// <see cref="SecondaryTitle"/> and <see cref="HeadingLevel"/> are ignored.
    /// </summary>
    [Parameter]
    public RenderFragment? TitleTemplate { get; set; }

    /// <summary>
    /// Gets or sets an optional render fragment rendered in the right-hand side of the header.
    /// Click events on this area do not propagate to the toggle handler.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderActions { get; set; }

    /// <summary>
    /// Gets or sets the body content shown when the section is expanded.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets whether the section is collapsed. Supports two-way binding.
    /// </summary>
    [Parameter]
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// Raised when <see cref="IsCollapsed"/> changes, enabling two-way binding.
    /// </summary>
    [Parameter]
    public EventCallback<bool> IsCollapsedChanged { get; set; }

    /// <summary>
    /// Raised after the section is toggled, providing the new collapsed state.
    /// </summary>
    [Parameter]
    public EventCallback<bool> Toggled { get; set; }

    /// <summary>
    /// Gets or sets the tooltip shown on the header toggle button.
    /// </summary>
    [Parameter]
    public string ExpanderTooltip { get; set; } = "Click to expand or collapse";

    /// <summary>
    /// Gets or sets the HTML id attribute. Auto-generated if not provided.
    /// </summary>
    [Parameter]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the section header toggle is disabled. The body content remains visible but cannot be collapsed.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Gets or sets any additional HTML attributes to be applied to the outer container element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        {
            Id = $"pd-section-{Interlocked.Increment(ref _idCounter)}";
        }
    }

    private async Task OnHeaderClickAsync()
    {
        IsCollapsed = !IsCollapsed;
        await IsCollapsedChanged.InvokeAsync(IsCollapsed).ConfigureAwait(true);
        await Toggled.InvokeAsync(IsCollapsed).ConfigureAwait(true);
    }

    /// <summary>
    /// Collapses the section.
    /// </summary>
    public async Task CollapseAsync()
    {
        if (!IsCollapsed)
        {
            await OnHeaderClickAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Expands the section.
    /// </summary>
    public async Task ExpandAsync()
    {
        if (IsCollapsed)
        {
            await OnHeaderClickAsync().ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Toggles the section between collapsed and expanded.
    /// </summary>
    public Task ToggleAsync() => OnHeaderClickAsync();
}
