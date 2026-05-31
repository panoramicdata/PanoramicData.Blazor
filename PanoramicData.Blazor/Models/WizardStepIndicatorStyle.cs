namespace PanoramicData.Blazor.Models;

/// <summary>
/// The visual style of the step progress indicator in a PDWizard.
/// </summary>
public enum WizardStepIndicatorStyle
{
    /// <summary>
    /// Numbered circles connected by a line (classic wizard style).
    /// </summary>
    Numbers,

    /// <summary>
    /// Small dots indicating current position (minimal/mobile-friendly).
    /// </summary>
    Dots,

    /// <summary>
    /// Horizontal breadcrumb-style step titles with separators.
    /// </summary>
    Breadcrumb
}
