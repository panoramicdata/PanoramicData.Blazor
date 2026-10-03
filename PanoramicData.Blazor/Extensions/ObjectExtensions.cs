namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for <see cref="object"/> values.
/// </summary>
public static class ObjectExtensions
{
	private static readonly Dictionary<Type, Func<string, object>> _parsers = new()
	{
		[typeof(Guid)] = text => Guid.Parse(text),
		[typeof(int?)] = text => int.Parse(text, CultureInfo.CurrentCulture),
		[typeof(DateTime)] = text => DateTime.Parse(text, CultureInfo.CurrentCulture),
		[typeof(DateTimeOffset)] = text => DateTimeOffset.Parse(text, CultureInfo.CurrentCulture)
	};

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
			return Enum.Parse(actualType, value.ToString() ?? string.Empty);
		}

		// a nullable int is parsed from its text, whereas a plain int is converted
		if (_parsers.TryGetValue(type, out var parser) || _parsers.TryGetValue(actualType, out parser))
		{
			return parser(value.ToString() ?? string.Empty);
		}

		return Convert.ChangeType(value, actualType, CultureInfo.CurrentCulture);
	}
}
