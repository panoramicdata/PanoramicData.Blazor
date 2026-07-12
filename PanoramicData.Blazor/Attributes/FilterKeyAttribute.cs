namespace PanoramicData.Blazor.Attributes;

/// <summary>
/// Specifies the filter key name used when generating filter expressions for a property.
/// When absent, the filter key defaults to the property name with its first character lower-cased.
/// </summary>
/// <param name="value">The explicit filter key to use instead of the default camel-cased property name.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FilterKeyAttribute(string value) : Attribute
{
	/// <summary>
	/// Gets or sets the filter key string.
	/// </summary>
	public string Value { get; set; } = value;

	/// <summary>
	/// Returns the filter key for the given property, using the attribute value when present or a camel-cased version of the property name otherwise.
	/// </summary>
	/// <param name="propertyInfo">The property to resolve the filter key for.</param>
	/// <returns>The filter key string.</returns>
	public static string Get(PropertyInfo propertyInfo) => propertyInfo.GetCustomAttributes()
			.OfType<FilterKeyAttribute>()
			.SingleOrDefault()?.Value ?? propertyInfo.Name[0].ToString().ToLowerInvariant() + propertyInfo.Name[1..];
}
