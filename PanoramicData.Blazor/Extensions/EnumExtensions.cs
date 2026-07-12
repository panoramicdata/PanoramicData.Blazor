namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for <see cref="System.Enum"/> values.
/// </summary>
public static class EnumExtensions
{
	/// <summary>
	/// Returns the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Name"/> of the enum member, or <c>null</c> when no attribute is applied.
	/// </summary>
	/// <param name="enumValue">The enum value to inspect.</param>
	/// <returns>The display name, or <c>null</c>.</returns>
	public static string? GetEnumDisplayName(this Enum enumValue)
		=> enumValue.GetType().GetMember(enumValue.ToString())
			?.FirstOrDefault()
			?.GetCustomAttribute<DisplayAttribute>()
			?.Name;

	/// <summary>
	/// Returns the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Description"/> of the enum member, or <c>null</c> when no attribute is applied.
	/// </summary>
	/// <param name="enumValue">The enum value to inspect.</param>
	/// <returns>The display description, or <c>null</c>.</returns>
	public static string? GetEnumDisplayDescription(this Enum enumValue)
		=> enumValue.GetType().GetMember(enumValue.ToString())
			?.FirstOrDefault()
			?.GetCustomAttribute<DisplayAttribute>()
			?.Description;

	/// <summary>
	/// Returns the <see cref="System.ComponentModel.DescriptionAttribute.Description"/> of the enum member, or <c>null</c> when no attribute is applied.
	/// </summary>
	/// <param name="enumValue">The enum value to inspect.</param>
	/// <returns>The description, or <c>null</c>.</returns>
	public static string? GetEnumDescription(this Enum enumValue)
		=> enumValue.GetType().GetMember(enumValue.ToString())
			?.FirstOrDefault()
			?.GetCustomAttribute<DescriptionAttribute>()
			?.Description;
}
