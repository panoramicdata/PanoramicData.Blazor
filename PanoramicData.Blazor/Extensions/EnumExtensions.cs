namespace PanoramicData.Blazor.Extensions;

public static class EnumExtensions
{
	public static string? GetEnumDisplayName(this Enum enumValue)
		=> enumValue.GetType().GetMember(enumValue.ToString())
			?.FirstOrDefault()
			?.GetCustomAttribute<DisplayAttribute>()
			?.Name;

	public static string? GetEnumDescription(this Enum enumValue)
		=> enumValue.GetType().GetMember(enumValue.ToString())
			?.FirstOrDefault()
			?.GetCustomAttribute<DescriptionAttribute>()
			?.Description;
}
