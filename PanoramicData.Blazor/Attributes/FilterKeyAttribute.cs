namespace PanoramicData.Blazor.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FilterKeyAttribute(string value) : Attribute
{
	public string Value { get; set; } = value;

	public static string Get(PropertyInfo propertyInfo) => propertyInfo.GetCustomAttributes()
			.OfType<FilterKeyAttribute>()
			.SingleOrDefault()?.Value ?? propertyInfo.Name[0].ToString().ToLowerInvariant() + propertyInfo.Name[1..];
}
