namespace PanoramicData.Blazor.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FilterKeyAttribute : Attribute
{
	public string Value { get; set; }

	public FilterKeyAttribute(string value)
	{
		Value = value;
	}

	public static string Get(PropertyInfo propertyInfo) => propertyInfo.GetCustomAttributes()
			.OfType<FilterKeyAttribute>()
			.SingleOrDefault()?.Value ?? propertyInfo.Name[0].ToString().ToLowerInvariant() + propertyInfo.Name[1..];
}
