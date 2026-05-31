namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDWizardPage
{
    // Basic wizard
    private string _basicName = string.Empty;
    private string? _basicResult;

    // Indicator style demos
    private string? _breadcrumbResult;
    private string? _dotsResult;

    // Async loading
    private string? _asyncResult;
    private string _asyncData = string.Empty;

    // Conditional step visibility
    private bool _showOptionalStep = true;
    private string? _conditionalResult;

    // Modal wizard
    private PDModal _wizardModal = null!;
    private PDModal _simpleModalWizard = null!;
    private PDWizard _modalWizard = null!;
    private string _modalDescription = string.Empty;
    private string? _modalResult;
    private int _modalJobCount = 3;
    private string _simpleModalName = string.Empty;
    private string? _simpleModalResult;

    private void OnBasicComplete()
    {
        _basicResult = $"Completed with name: {_basicName}";
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
    private PDModal _fixedHeightModal = null!;
    private string? _fixedHeightResult;

    private async Task OnFixedHeightComplete()
    {
        _fixedHeightResult = "Completed!";
        await _fixedHeightModal.HideAsync().ConfigureAwait(true);
    }

    private async Task OnFixedHeightCancel()
    {
        _fixedHeightResult = "Cancelled";
        await _fixedHeightModal.HideAsync().ConfigureAwait(true);
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
    private int _titleDemoItemCount = 5;

    private void OnTitleDemoComplete() => _titleDemoResult = "Completed!";
    private void OnTitleDemoCancel() => _titleDemoResult = "Cancelled";

    private async Task SimulateLoadAsync()
    {
        await Task.Delay(1500).ConfigureAwait(true);
        _asyncData = "(fetched at " + DateTime.Now.ToString("HH:mm:ss") + ")";
    }

    private async Task OnSimpleModalComplete()
    {
        _simpleModalResult = $"Completed with name: {_simpleModalName}";
        await _simpleModalWizard.HideAsync().ConfigureAwait(true);
    }

    private async Task OnSimpleModalCancel()
    {
        _simpleModalResult = "Cancelled";
        await _simpleModalWizard.HideAsync().ConfigureAwait(true);
    }

    private async Task OnModalWizardComplete()
    {
        _modalResult = $"Completed with description: {_modalDescription}";
        await _wizardModal.HideAsync().ConfigureAwait(true);
    }

    private async Task OnModalWizardCancel()
    {
        await _wizardModal.HideAsync().ConfigureAwait(true);
    }
}
