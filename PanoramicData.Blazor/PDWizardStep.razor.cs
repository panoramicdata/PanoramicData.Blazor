namespace PanoramicData.Blazor;

/// <summary>
/// Represents a single step inside a PDWizard component.
/// </summary>
public partial class PDWizardStep : ComponentBase
{
    /// <summary>
    /// Gets or sets the parent wizard component.
    /// </summary>
    [CascadingParameter(Name = "Wizard")] public PDWizard Wizard { get; set; } = default!;

    /// <summary>
    /// Gets or sets the display title of this step.
    /// </summary>
    [Parameter] public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional FontAwesome icon class (e.g. "fa-solid fa-upload") shown in the step indicator.
    /// </summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to this step's body container.
    /// </summary>
    [Parameter] public string CssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this step is visible. When false the step is skipped entirely.
    /// </summary>
    [Parameter] public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional predicate evaluated before allowing the user to proceed past this step.
    /// Return false to keep the Next/Finish button disabled.
    /// </summary>
    [Parameter] public Func<bool>? CanProceed { get; set; }

    /// <summary>
    /// Gets or sets an async callback invoked when this step becomes active (e.g. to load data).
    /// While running, a loading overlay is shown.
    /// </summary>
    [Parameter] public Func<Task>? OnEnterAsync { get; set; }

    /// <summary>
    /// Gets or sets an async callback invoked when the user leaves this step (before moving forward).
    /// </summary>
    [Parameter] public Func<Task>? OnLeaveAsync { get; set; }

    /// <summary>
    /// Gets or sets optional content shown while <see cref="OnEnterAsync"/> is running.
    /// When null a default spinner is shown.
    /// </summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>
    /// Gets or sets the content of this step.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets whether this step is currently loading (OnEnterAsync in progress).
    /// </summary>
    internal bool IsLoading { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Wizard?.AddStep(this);
    }
}
