namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDWizardPage
{
    // Result message of each demo wizard, keyed by demo name
    private readonly Dictionary<string, string> _results = [];

    // Basic wizard
    protected string BasicName { get; set; } = string.Empty;

    // Async loading
    private string _asyncData = string.Empty;

    // Conditional step visibility
    protected bool ShowOptionalStep { get; set; } = true;

    // Modal wizard
    protected PDModal WizardModal { get; set; } = null!;
    protected PDModal SimpleModalWizard { get; set; } = null!;
    protected PDWizard ModalWizard { get; set; } = null!;
    protected string ModalDescription { get; set; } = string.Empty;
    protected int ModalJobCount { get; set; } = 3;
    protected string SimpleModalName { get; set; } = string.Empty;

    // Fixed body height demo
    protected PDModal FixedHeightModal { get; set; } = null!;

    // Extra button demo
    private string? _extraButtonSaveResult;

    // Title bar / no-indicator demo
    protected int TitleDemoItemCount { get; set; } = 5;

    private void Complete(string demo) => _results[demo] = "Completed!";

    private void Cancel(string demo) => _results[demo] = "Cancelled";

    private void OnBasicComplete()
    {
        _results["basic"] = $"Completed with name: {BasicName}";
    }

    private async Task OnFixedHeightComplete()
    {
        Complete("fixedHeight");
        await FixedHeightModal.HideAsync().ConfigureAwait(true);
    }

    private async Task OnFixedHeightCancel()
    {
        Cancel("fixedHeight");
        await FixedHeightModal.HideAsync().ConfigureAwait(true);
    }

    private void OnExtraButtonSaveDraft() => _extraButtonSaveResult = "Draft saved at " + DateTime.Now.ToString("HH:mm:ss");

    private async Task SimulateLoadAsync()
    {
        await Task.Delay(1500).ConfigureAwait(true);
        _asyncData = "(fetched at " + DateTime.Now.ToString("HH:mm:ss") + ")";
    }

    private async Task OnSimpleModalComplete()
    {
        _results["simpleModal"] = $"Completed with name: {SimpleModalName}";
        await SimpleModalWizard.HideAsync().ConfigureAwait(true);
    }

    private async Task OnSimpleModalCancel()
    {
        Cancel("simpleModal");
        await SimpleModalWizard.HideAsync().ConfigureAwait(true);
    }

    private async Task OnModalWizardComplete()
    {
        _results["modal"] = $"Completed with description: {ModalDescription}";
        await WizardModal.HideAsync().ConfigureAwait(true);
    }

    private async Task OnModalWizardCancel()
    {
        await WizardModal.HideAsync().ConfigureAwait(true);
    }
}
