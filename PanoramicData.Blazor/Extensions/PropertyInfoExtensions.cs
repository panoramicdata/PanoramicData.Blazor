namespace PanoramicData.Blazor.Extensions;

public static class PropertyInfoExtensions
{
	public static string? GetDisplayShortName(this PropertyInfo propertyInfo) => propertyInfo.GetCustomAttributes()
		.OfType<DisplayAttribute>()
		.SingleOrDefault()?.ShortName?.LowerFirstChar();

	public static string? GetFilterKey(this PropertyInfo propertyInfo) => propertyInfo.GetCustomAttributes()
		.OfType<FilterKeyAttribute>()
		.SingleOrDefault()?.Value?.LowerFirstChar();

}
