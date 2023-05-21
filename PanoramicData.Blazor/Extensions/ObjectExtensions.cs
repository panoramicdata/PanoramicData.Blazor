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
		else if (type.IsEnum)
		{
			return Enum.Parse(type, value?.ToString() ?? String.Empty);
		}
		else if (type.FullName == "System.Guid")
		{
			return Guid.Parse(value.ToString() ?? String.Empty);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.Int32", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (Int32?)Int32.Parse(value.ToString() ?? String.Empty);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.Guid", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (Guid?)Guid.Parse(value.ToString() ?? String.Empty);
		}
		else if (type.FullName == "System.DateTime")
		{
			return DateTime.Parse(value.ToString() ?? String.Empty, CultureInfo.CurrentCulture);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.DateTime", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (DateTime?)DateTime.Parse(value.ToString() ?? String.Empty, CultureInfo.CurrentCulture);
		}
		else if (type.FullName == "System.DateTimeOffset")
		{
			return DateTimeOffset.Parse(value.ToString() ?? String.Empty, CultureInfo.CurrentCulture);
		}
		else if (type.FullName?.StartsWith("System.Nullable`1[[System.DateTimeOffset", StringComparison.InvariantCultureIgnoreCase) == true)
		{
			return (DateTimeOffset?)DateTimeOffset.Parse(value.ToString() ?? String.Empty, CultureInfo.CurrentCulture);
		}

		return Convert.ChangeType(value, type, CultureInfo.CurrentCulture);
	}
}
