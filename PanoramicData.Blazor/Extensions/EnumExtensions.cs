namespace PanoramicData.Blazor.Extensions;

public static class EnumExtensions
{
	public static string? GetEnumDisplayName(this Enum enumType)
	{
		return enumType.GetType().GetMember(enumType.ToString())
					   ?.FirstOrDefault()
					   ?.GetCustomAttribute<DisplayAttribute>()
					   ?.Name;
	}
}
