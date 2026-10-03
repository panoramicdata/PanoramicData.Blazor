namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDWizardPage
{
    // Basic wizard
    protected string BasicName { get; set; } = string.Empty;
    private string? _basicResult;

    // Indicator style demos
    private string? _breadcrumbResult;
    private string? _dotsResult;

    // Async loading
    private string? _asyncResult;
    private string _asyncData = string.Empty;

    // Conditional step visibility
    protected bool ShowOptionalStep { get; set; } = true;
    private string? _conditionalResult;

    // Modal wizard
    protected PDModal WizardModal { get; set; } = null!;
    protected PDModal SimpleModalWizard { get; set; } = null!;
    protected PDWizard ModalWizard { get; set; } = null!;
    protected string ModalDescription { get; set; } = string.Empty;
    private string? _modalResult;
    protected int ModalJobCount { get; set; } = 3;
    protected string SimpleModalName { get; set; } = string.Empty;
    private string? _simpleModalResult;

    private void OnBasicComplete()
    {
        _basicResult = $"Completed with name: {BasicName}";
    }

    private void OnBasicCancel()
    {
        _basicResult = "Cancelled";
    }

    private void OnBreadcrumbComplete() => _breadcrumbResult = "Completed!";
    private void OnBreadcrumbCancel() => _breadcrumbResult = "Cancelled";

    private void OnDotsComplete() => _dotsResult = "Completed!";
    private void OnDotsCancel() => _dotsResult = "Cancelled";

    private void OnAsyncComplete() => _asyncResult = "Completed!";
    private void OnAsyncCancel() => _asyncResult = "Cancelled";

    private void OnConditionalComplete() => _conditionalResult = "Completed!";
    private void OnConditionalCancel() => _conditionalResult = "Cancelled";

    // CSS theming demo
    private string? _themingResult;

    private void OnThemingComplete() => _themingResult = "Completed!";
    private void OnThemingCancel() => _themingResult = "Cancelled";

    // Fixed body height demo
    protected PDModal FixedHeightModal { get; set; } = null!;
    private string? _fixedHeightResult;

    private async Task OnFixedHeightComplete()
    {
        _fixedHeightResult = "Completed!";
        await FixedHeightModal.HideAsync().ConfigureAwait(true);
    }

    private async Task OnFixedHeightCancel()
    {
        _fixedHeightResult = "Cancelled";
        await FixedHeightModal.HideAsync().ConfigureAwait(true);
    }

    // Custom button icons demo
    private string? _iconDemoResult;

    private void OnIconDemoComplete() => _iconDemoResult = "Completed!";
    private void OnIconDemoCancel() => _iconDemoResult = "Cancelled";

    // Extra button demo
    private string? _extraButtonResult;
    private string? _extraButtonSaveResult;

    private void OnExtraButtonComplete() => _extraButtonResult = "Completed!";
    private void OnExtraButtonCancel() => _extraButtonResult = "Cancelled";
    private void OnExtraButtonSaveDraft() => _extraButtonSaveResult = "Draft saved at " + DateTime.Now.ToString("HH:mm:ss");

    // Title bar / no-indicator demo
    private string? _titleDemoResult;
    protected int TitleDemoItemCount { get; set; } = 5;

    private void OnTitleDemoComplete() => _titleDemoResult = "Completed!";
    private void OnTitleDemoCancel() => _titleDemoResult = "Cancelled";

    // Custom completed step icons demo
    private string? _customIconResult;

    private void OnCustomIconComplete() => _customIconResult = "Completed!";
    private void OnCustomIconCancel() => _customIconResult = "Cancelled";

    private async Task SimulateLoadAsync()
    {
        await Task.Delay(1500).ConfigureAwait(true);
        _asyncData = "(fetched at " + DateTime.Now.ToString("HH:mm:ss") + ")";
    }

    private async Task OnSimpleModalComplete()
    {
        _simpleModalResult = $"Completed with name: {SimpleModalName}";
        await SimpleModalWizard.HideAsync().ConfigureAwait(true);
    }

    private async Task OnSimpleModalCancel()
    {
        _simpleModalResult = "Cancelled";
        await SimpleModalWizard.HideAsync().ConfigureAwait(true);
    }

    private async Task OnModalWizardComplete()
    {
        _modalResult = $"Completed with description: {ModalDescription}";
        await WizardModal.HideAsync().ConfigureAwait(true);
    }

    private async Task OnModalWizardCancel()
    {
        await WizardModal.HideAsync().ConfigureAwait(true);
    }
}
