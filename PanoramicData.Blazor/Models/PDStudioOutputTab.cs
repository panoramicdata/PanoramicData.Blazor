namespace PanoramicData.Blazor.Models;

/// <summary>
/// A tab a host adds after <see cref="PDStudio"/>'s Results and Messages tabs, such as a query plan or a chart.
/// </summary>
/// <param name="Key">Identifies the tab, for <see cref="PDStudio.SelectTab(string)"/>; must not be "results" or "messages".</param>
/// <param name="Title">The tab's text.</param>
/// <param name="Content">The tab's content.</param>
/// <param name="IconCssClass">An icon shown before the title, such as "fas fa-sitemap"; null for none.</param>
/// <param name="ToolTip">The tab's tooltip; null for none.</param>
public record PDStudioOutputTab(string Key, string Title, RenderFragment Content, string? IconCssClass = null, string? ToolTip = null)
{
	/// <summary>
	/// Gets a badge shown after the tab's title, such as a count or a dot for a status; null for none. To change it,
	/// give the studio the tab again with <c>with { Badge = ... }</c>.
	/// </summary>
	public PDStudioTabBadge? Badge { get; init; }
}
