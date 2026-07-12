namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for <see cref="System.Reflection.PropertyInfo"/>.
/// </summary>
public static class PropertyInfoExtensions
{
	/// <summary>
	/// Returns the camel-cased <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.ShortName"/> of the property, or <c>null</c> when no attribute is applied.
	/// </summary>
	/// <param name="propertyInfo">The property to inspect.</param>
	/// <returns>The short display name with its first character lower-cased, or <c>null</c>.</returns>
	public static string? GetDisplayShortName(this PropertyInfo propertyInfo) => propertyInfo.GetCustomAttributes()
		.OfType<DisplayAttribute>()
		.SingleOrDefault()?.ShortName?.LowerFirstChar();

	/// <summary>
	/// Returns the camel-cased <see cref="PanoramicData.Blazor.Attributes.FilterKeyAttribute"/> value of the property, or <c>null</c> when no attribute is applied.
	/// </summary>
	/// <param name="propertyInfo">The property to inspect.</param>
	/// <returns>The filter key with its first character lower-cased, or <c>null</c>.</returns>
	public static string? GetFilterKey(this PropertyInfo propertyInfo) => propertyInfo.GetCustomAttributes()
		.OfType<FilterKeyAttribute>()
		.SingleOrDefault()?.Value?.LowerFirstChar();

}
