namespace PanoramicData.Blazor;

/// <summary>
/// A multi-step wizard component. Define steps with child <see cref="PDWizardStep"/> components.
/// </summary>
public partial class PDWizard : ComponentBase
{
    private static int _sequence;
    private readonly List<PDWizardStep> _steps = [];
    private int _activeStepIndex;

    /// <summary>
    /// Gets the unique identifier of this wizard instance.
    /// </summary>
    [Parameter] public string Id { get; set; } = $"pd-wizard-{++_sequence}";

    /// <summary>
    /// Gets or sets additional HTML attributes (e.g. style, data-*) applied to the root element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets or sets the child content (PDWizardStep components).
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the root element.
    /// </summary>
    [Parameter] public string CssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional CSS classes applied to the step indicator bar.
    /// </summary>
    [Parameter] public string IndicatorCssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional CSS classes applied to the wizard body.
    /// </summary>
    [Parameter] public string BodyCssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional CSS classes applied to the wizard footer.
    /// </summary>
    [Parameter] public string FooterCssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the visual style of the step progress indicator.
    /// </summary>
    [Parameter] public WizardStepIndicatorStyle StepIndicatorStyle { get; set; } = WizardStepIndicatorStyle.Numbers;

    /// <summary>
    /// Gets or sets whether step titles are shown beneath the step circles (Numbers style only).
    /// </summary>
    [Parameter] public bool ShowStepTitles { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the step progress indicator bar is rendered.
    /// </summary>
    [Parameter] public bool ShowIndicator { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the wizard footer is rendered.
    /// </summary>
    [Parameter] public bool ShowFooter { get; set; } = true;

    /// <summary>
    /// Gets or sets whether completed steps in the indicator are clickable for backwards navigation.
    /// </summary>
    [Parameter] public bool AllowStepNavigation { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional template rendered as the left-side title above the wizard body.
    /// Receives the current <see cref="PDWizardStep"/> as context. When null no title bar is rendered.
    /// </summary>
    [Parameter] public RenderFragment<PDWizardStep?>? TitleTemplate { get; set; }

    /// <summary>
    /// Gets or sets an optional template rendered on the right side of the title bar.
    /// Receives the current <see cref="PDWizardStep"/> as context. Automatically pushed to the right.
    /// </summary>
    [Parameter] public RenderFragment<PDWizardStep?>? TitleAddon { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes applied to the right-side title addon element.
    /// Defaults to "text-secondary".
    /// </summary>
    [Parameter] public string TitleAddonCssClass { get; set; } = "text-secondary";

    /// <summary>
    /// Gets or sets the label for the Back button.
    /// </summary>
    [Parameter] public string BackButtonText { get; set; } = "Back";

    /// <summary>
    /// Gets or sets the label for the Next button.
    /// </summary>
    [Parameter] public string NextButtonText { get; set; } = "Next";

    /// <summary>
    /// Gets or sets the label for the Finish button (shown on the last step).
    /// </summary>
    [Parameter] public string FinishButtonText { get; set; } = "Finish";

    /// <summary>
    /// Gets or sets the label for the Cancel button.
    /// </summary>
    [Parameter] public string CancelButtonText { get; set; } = "Cancel";

    /// <summary>
    /// Gets or sets the FontAwesome icon class for the Cancel button. Set to null to suppress the icon.
    /// </summary>
    [Parameter] public string? CancelIcon { get; set; }

    /// <summary>
    /// Gets or sets the FontAwesome icon class for the Back button. Set to null to suppress the icon.
    /// </summary>
    [Parameter] public string? BackIcon { get; set; } = "fa fa-solid fa-arrow-left";

    /// <summary>
    /// Gets or sets the FontAwesome icon class for the Next button. Set to null to suppress the icon.
    /// </summary>
    [Parameter] public string? NextIcon { get; set; } = "fa fa-solid fa-arrow-right";

    /// <summary>
    /// Gets or sets the FontAwesome icon class for the Finish button. Set to null to suppress the icon.
    /// </summary>
    [Parameter] public string? FinishIcon { get; set; } = "fa fa-solid fa-check";

    /// <summary>
    /// Gets or sets optional content to replace the default footer entirely.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Gets or sets an optional extra button rendered in the footer between the Cancel button
    /// and the Back/Next/Finish group. Use this for secondary actions such as "Save as Draft".
    /// </summary>
    [Parameter] public RenderFragment? ExtraButton { get; set; }

    /// <summary>
    /// Gets the zero-based index of the currently active step (among visible steps).
    /// </summary>
    public int CurrentStepIndex => _activeStepIndex;

    /// <summary>
    /// Gets the currently active <see cref="PDWizardStep"/>, or null if no steps are registered.
    /// </summary>
    public PDWizardStep? CurrentStep => _steps.Where(s => s.IsVisible).ElementAtOrDefault(_activeStepIndex);

    /// <summary>
    /// An event callback invoked when the step changes. Argument is the new zero-based step index.
    /// </summary>
    [Parameter] public EventCallback<int> StepChanged { get; set; }

    /// <summary>
    /// An event callback invoked when the user clicks Finish on the last step.
    /// </summary>
    [Parameter] public EventCallback OnComplete { get; set; }

    /// <summary>
    /// An event callback invoked when the user clicks Cancel.
    /// </summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>
    /// Registers a step. Called by PDWizardStep.OnInitialized.
    /// </summary>
    internal void AddStep(PDWizardStep step)
    {
        if (!_steps.Contains(step))
        {
            _steps.Add(step);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Returns true if the given step is currently active.
    /// </summary>
    internal bool IsActiveStep(PDWizardStep step)
    {
        var visible = _steps.Where(s => s.IsVisible).ToList();
        var activeStep = visible.ElementAtOrDefault(_activeStepIndex);
        return ReferenceEquals(step, activeStep);
    }

    /// <summary>
    /// Advances to the next visible step.
    /// </summary>
    public async Task NextAsync()
    {
        var visible = _steps.Where(s => s.IsVisible).ToList();
        var current = visible.ElementAtOrDefault(_activeStepIndex);

        if (current?.OnLeaveAsync != null)
        {
            await current.OnLeaveAsync().ConfigureAwait(true);
        }

        if (_activeStepIndex < visible.Count - 1)
        {
            await GoToStepAsync(_activeStepIndex + 1).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Returns to the previous visible step.
    /// </summary>
    public async Task BackAsync()
    {
        if (_activeStepIndex > 0)
        {
            await GoToStepAsync(_activeStepIndex - 1).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Navigates to the step at the given zero-based visible-step index.
    /// </summary>
    public async Task GoToStepAsync(int index)
    {
        var visible = _steps.Where(s => s.IsVisible).ToList();
        if (index < 0 || index >= visible.Count)
        {
            return;
        }

        _activeStepIndex = index;
        await StepChanged.InvokeAsync(_activeStepIndex).ConfigureAwait(true);

        var step = visible[index];
        if (step.OnEnterAsync != null)
        {
            step.IsLoading = true;
            StateHasChanged();
            try
            {
                await step.OnEnterAsync().ConfigureAwait(true);
            }
            finally
            {
                step.IsLoading = false;
                StateHasChanged();
            }
        }
        else
        {
            StateHasChanged();
        }
    }

    /// <summary>
    /// Resets the wizard back to the first step.
    /// </summary>
    public async Task ResetAsync()
    {
        _activeStepIndex = 0;
        await StepChanged.InvokeAsync(0).ConfigureAwait(true);
        StateHasChanged();
    }

    private async Task OnIndicatorClickAsync(int index)
    {
        if (!AllowStepNavigation)
        {
            return;
        }

        // Only allow jumping to already-completed steps
        if (index < _activeStepIndex)
        {
            await GoToStepAsync(index).ConfigureAwait(true);
        }
    }

    private async Task OnFinishClickAsync()
    {
        var visible = _steps.Where(s => s.IsVisible).ToList();
        var current = visible.ElementAtOrDefault(_activeStepIndex);

        if (current?.OnLeaveAsync != null)
        {
            await current.OnLeaveAsync().ConfigureAwait(true);
        }

        await OnComplete.InvokeAsync().ConfigureAwait(true);
        await ResetAsync().ConfigureAwait(true);
    }

    private async Task OnCancelClickAsync()
    {
        await OnCancel.InvokeAsync().ConfigureAwait(true);
        await ResetAsync().ConfigureAwait(true);
    }
}
