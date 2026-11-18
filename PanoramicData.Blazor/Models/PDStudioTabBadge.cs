namespace PanoramicData.Blazor.Models;

/// <summary>
/// A badge on one of a <see cref="PDStudio"/> host's tabs: a short text such as a count, or a dot when it has none.
/// </summary>
/// <param name="Text">The badge's text, such as "3"; null or empty shows a dot.</param>
public record PDStudioTabBadge(string? Text = null)
{
	/// <summary>Gets the badge's kind, which sets its colour.</summary>
	public PDStudioTabBadgeKind Kind { get; init; } = PDStudioTabBadgeKind.Neutral;

	/// <summary>Gets classes added to the badge, for a style of the host's own.</summary>
	public string? CssClass { get; init; }

	/// <summary>Gets the badge's tooltip, which a dot needs to say what it means.</summary>
	public string? ToolTip { get; init; }
}

/// <summary>What a <see cref="PDStudioTabBadge"/> tells, which sets its colour.</summary>
public enum PDStudioTabBadgeKind
{
	/// <summary>Nothing in particular, such as a count.</summary>
	Neutral,

	/// <summary>Something under way, or worth knowing.</summary>
	Info,

	/// <summary>Something that went well.</summary>
	Success,

	/// <summary>Something that needs a look.</summary>
	Warning,

	/// <summary>Something that went wrong.</summary>
	Error
}
