namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a named status with its associated icon and colour, used by PDStatusCascade.
/// Consumers can define custom statuses via <see cref="Custom"/>.
/// </summary>
public sealed class StatusType
{
    /// <summary>Gets the unique name used when serialising to JSON / CSS lookup.</summary>
    public string Name { get; }

    /// <summary>Gets the default Font Awesome icon class (e.g. "fas fa-check-circle").</summary>
    public string DefaultIconClass { get; }

    /// <summary>Gets the default CSS colour class applied to the icon (e.g. "text-success").</summary>
    public string DefaultColorClass { get; }

    private StatusType(string name, string iconClass, string colorClass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(iconClass);
        ArgumentException.ThrowIfNullOrWhiteSpace(colorClass);
        Name = name;
        DefaultIconClass = iconClass;
        DefaultColorClass = colorClass;
    }

    // ── Built-in statuses ─────────────────────────────────────────────────────

    /// <summary>Error / failure state — red.</summary>
    public static readonly StatusType Red = new("red", "fas fa-times-circle", "text-danger");

    /// <summary>Warning / degraded state — amber.</summary>
    public static readonly StatusType Amber = new("amber", "fas fa-exclamation-triangle", "pdsc-icon-amber");

    /// <summary>Healthy / success state — green.</summary>
    public static readonly StatusType Green = new("green", "fas fa-check-circle", "text-success");

    /// <summary>Unknown / not yet evaluated state — gray.</summary>
    public static readonly StatusType Gray = new("gray", "fas fa-question-circle", "text-secondary");

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a consumer-defined status with a custom name, icon, and colour class.
    /// </summary>
    /// <param name="name">Unique identifier (used in serialisation and CSS lookup).</param>
    /// <param name="iconClass">Font Awesome (or other) CSS class for the icon.</param>
    /// <param name="colorClass">CSS class applied to the icon element for colour.</param>
    public static StatusType Custom(string name, string iconClass, string colorClass)
        => new(name, iconClass, colorClass);

    /// <inheritdoc/>
    public override string ToString() => Name;
}
