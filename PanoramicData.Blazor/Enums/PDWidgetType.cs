namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Defines the content types available for a PDWidget.
/// </summary>
public enum PDWidgetType
{
	/// <summary>
	/// Render sanitized HTML content directly.
	/// </summary>
	Html,

	/// <summary>
	/// Fetch URL content server-side and render (no iframe).
	/// </summary>
	Url,

	/// <summary>
	/// Real-time clock display with configurable timezone/format.
	/// </summary>
	Clock,

	/// <summary>
	/// Display image from byte array or URL.
	/// </summary>
	Image,

	/// <summary>
	/// Render a ChildContent RenderFragment provided by the consumer.
	/// </summary>
	Custom
}
