namespace PanoramicData.Blazor.Extensions;

public static class ObjectExtensions
{
	/// <summary>
	/// Inspects the incoming data type and casts to the requested type.
	/// </summary>
	/// <param name="value">The object value being cast.</param>
	/// <param name="type">The data type to be cast to.</param>
	/// <returns></returns>
	public static object? Cast(this object? value, Type type)
	{
		if (value is null)
		{
			return null;
		}

		var actualType = Nullable.GetUnderlyingType(type) ?? type;

		if (actualType.IsEnum)
		{
			return Enum.Parse(actualType, value?.ToString() ?? string.Empty);
		}
		else if (actualType.FullName == "System.Guid")
		{
			return Guid.Parse(value.ToString() ?? string.Empty);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.Int32", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (int?)int.Parse(value.ToString() ?? string.Empty, CultureInfo.CurrentCulture);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.Guid", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (Guid?)Guid.Parse(value.ToString() ?? string.Empty);
		}
		else if (actualType.FullName == "System.DateTime")
		{
			return DateTime.Parse(value.ToString() ?? string.Empty, CultureInfo.CurrentCulture);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.DateTimeOffset", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (DateTimeOffset?)DateTimeOffset.Parse(value.ToString() ?? string.Empty, CultureInfo.CurrentCulture);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.DateTime", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (DateTime?)DateTime.Parse(value.ToString() ?? string.Empty, CultureInfo.CurrentCulture);
		}
		else if (actualType.FullName == "System.DateTimeOffset")
		{
			return DateTimeOffset.Parse(value.ToString() ?? string.Empty, CultureInfo.CurrentCulture);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.DateTimeOffset", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (DateTimeOffset?)DateTimeOffset.Parse(value.ToString() ?? string.Empty, CultureInfo.CurrentCulture);
		}

		return Convert.ChangeType(value, actualType, CultureInfo.CurrentCulture);
	}
}
