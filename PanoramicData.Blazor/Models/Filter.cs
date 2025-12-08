namespace PanoramicData.Blazor.Models;

public class Filter
{
	private static readonly string[] _formatsWithTimeZone =
	[
		"yyyy'-'MM'-'dd HH:m zzz",
		"yyyy'-'MM'-'dd HH:mm zzz",
		"yyyy'-'MM'-'dd HH:mm:ss zzz",
		"yyyy'-'MM'-'dd zzz",
		"yyyy'-'MM'-'dd HH:mm:ss K",

		"yyyy'-'MM'-'dd HH:m.fff zzz",
		"yyyy'-'MM'-'dd HH:mm.fff zzz",
		"yyyy'-'MM'-'dd HH:mm:ss.fff zzz",
		"yyyy'-'MM'-'dd zzz",
		"yyyy'-'MM'-'dd HH:mm:ss.fff K",

		"dd'/'MM'/'yyyy HH:m zzz",
		"dd'/'MM'/'yyyy HH:mm zzz",
		"dd'/'MM'/'yyyy HH:mm:ss zzz",
		"dd'/'MM'/'yyyy zzz",
		"dd'/'MM'/'yyyy HH:mm:ss K",

		"dd'-'MM'-'yyyy HH:m zzz",
		"dd'-'MM'-'yyyy HH:mm zzz",
		"dd'-'MM'-'yyyy HH:mm:ss zzz",
		"dd'-'MM'-'yyyy zzz",
		"dd'-'MM'-'yyyy HH:mm:ss K",

		"dd'/'MM'/'yyyy HH:m.fff zzz",
		"dd'/'MM'/'yyyy HH:mm.fff zzz",
		"dd'/'MM'/'yyyy HH:mm:ss.fff zzz",
		"dd'/'MM'/'yyyy zzz",
		"dd'/'MM'/'yyyy HH:mm:ss.fff K",

		"MM'/'dd'/'yyyy HH:m zzz",
		"MM'/'dd'/'yyyy HH:mm zzz",
		"MM'/'dd'/'yyyy HH:mm:ss zzz",
		"MM'/'dd'/'yyyy zzz",
		"MM'/'dd'/'yyyy HH:mm:ss K",

		"MM'/'dd'/'yyyy HH:m.fff zzz",
		"MM'/'dd'/'yyyy HH:mm.fff zzz",
		"MM'/'dd'/'yyyy HH:mm:ss.fff zzz",
		"MM'/'dd'/'yyyy zzz",
		"MM'/'dd'/'yyyy HH:mm:ss.fff K"
	];

	private static readonly string[] _formatsWithoutTimeZone =
	[
		"yyyy'-'MM'-'dd HH:m",
		"yyyy'-'MM'-'dd HH:mm",
		"yyyy'-'MM'-'dd HH:mm:ss",
		"yyyy'-'MM'-'dd",
		"yyyy'-'MM'-'dd HH:mm:ss K",

		"yyyy'-'MM'-'dd HH:m.fff",
		"yyyy'-'MM'-'dd HH:mm.fff",
		"yyyy'-'MM'-'dd HH:mm:ss.fff",
		"yyyy'-'MM'-'dd",
		"yyyy'-'MM'-'dd HH:mm:ss.fff K",

		"dd'/'MM'/'yyyy HH:m",
		"dd'/'MM'/'yyyy HH:mm",
		"dd'/'MM'/'yyyy HH:mm:ss",
		"dd'/'MM'/'yyyy HH:mm:ss K",
		"dd'/'MM'/'yyyy",

		"dd'-'MM'-'yyyy HH:m",
		"dd'-'MM'-'yyyy HH:mm",
		"dd'-'MM'-'yyyy HH:mm:ss",
		"dd'-'MM'-'yyyy HH:mm:ss K",
		"dd'-'MM'-'yyyy",

		"dd'/'MM'/'yyyy HH:m.fff",
		"dd'/'MM'/'yyyy HH:mm.fff",
		"dd'/'MM'/'yyyy HH:mm:ss.fff",
		"dd'/'MM'/'yyyy HH:mm:ss.fff K",
		"dd'/'MM'/'yyyy",

		"MM'/'yy'/'yyyy HH:m",
		"MM'/'yy'/'yyyy HH:mm",
		"MM'/'yy'/'yyyy HH:mm:ss",
		"MM'/'yy'/'yyyy HH:mm:ss K",
		"MM'/'yy'/'yyyy",

		"MM'/'yy'/'yyyy HH:m.fff",
		"MM'/'yy'/'yyyy HH:mm.fff",
		"MM'/'yy'/'yyyy HH:mm:ss.fff",
		"MM'/'yy'/'yyyy HH:mm:ss.fff K",
		"MM'/'yy'/'yyyy",
	];

	public Filter()
	{
	}

	public Filter(FilterTypes filterType, string key, string value)
	{
		FilterType = filterType;
		Key = key;
		Value = value;
	}

	public Filter(FilterTypes filterType, string key, object value)
	{
		FilterType = filterType;
		Key = key;
		Value = value?.ToString() ?? string.Empty;
	}


	public Filter(FilterTypes filterType, string key, string value, string value2)
	{
		FilterType = filterType;
		Key = key;
		Value = value;
		Value2 = value2;
	}

	public FilterTypes FilterType { get; set; }

	public string Key { get; set; } = string.Empty;

	public string PropertyName { get; set; } = string.Empty;

	public string Value { get; set; } = string.Empty;

	public string Value2 { get; set; } = string.Empty;

	public KeyValuePair<string, object> Values { get; set; }

	public bool UnspecifiedDateTimesAreUtc { get; set; } = true;

	public void Clear()
	{
		FilterType = FilterTypes.Equals;
		Value = string.Empty;
	}

	public bool IsValid => FilterType switch
	{
		FilterTypes.Range => !string.IsNullOrWhiteSpace(Value) && !string.IsNullOrWhiteSpace(Value2),
		FilterTypes.IsNull => true,
		FilterTypes.IsNotNull => true,
		FilterTypes.IsEmpty => true,
		FilterTypes.IsNotEmpty => true,
		_ => !string.IsNullOrWhiteSpace(Value)
	};

	public override string ToString()
	{
		return FilterType switch
		{
			FilterTypes.Equals => $"{Key}:{Value}",
			FilterTypes.DoesNotEqual => $"{Key}:!{Value}",
			FilterTypes.StartsWith => $"{Key}:{Value}*",
			FilterTypes.EndsWith => $"{Key}:*{Value}",
			FilterTypes.Contains => $"{Key}:*{Value}*",
			FilterTypes.DoesNotContain => $"{Key}:!*{Value}*",
			FilterTypes.In => $"{Key}:In({Value})",
			FilterTypes.NotIn => $"{Key}:!In({Value})",
			FilterTypes.GreaterThan => $"{Key}:>{Value}",
			FilterTypes.GreaterThanOrEqual => $"{Key}:>={Value}",
			FilterTypes.LessThan => $"{Key}:<{Value}",
			FilterTypes.LessThanOrEqual => $"{Key}:<={Value}",
			FilterTypes.Range => $"{Key}:>{Value}|{Value2}<",
			FilterTypes.IsNull => $"{Key}:(null)",
			FilterTypes.IsNotNull => $"{Key}:!(null)",
			FilterTypes.IsEmpty => $"{Key}:(empty)",
			FilterTypes.IsNotEmpty => $"{Key}:!(empty)",
			_ => string.Empty,
		};
	}

	public void UpdateFrom(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			FilterType = FilterTypes.Equals;
			Value = string.Empty;
		}
		else
		{
			// Parse filters lazily and find the one matching this key (case-insensitive)
			var matchingFilter = ParseMany(text)
				.FirstOrDefault(f => string.Equals(f.Key, Key, StringComparison.OrdinalIgnoreCase));
			
			if (matchingFilter is null)
			{
				Clear();
			}
			else
			{
				FilterType = matchingFilter.FilterType;
				Value = matchingFilter.Value;
				Value2 = matchingFilter.Value2;
				Values = matchingFilter.Values;
			}
		}
	}

	#region Class Members

	public static string Format(object value) => Format(value, false);

	public static string Format(object value, bool unspecifiedDateTimesAreUtc)
	{
		if (value is null)
		{
			return "";
		}

		if (value is DateTime dt)
		{
			return dt.Kind == DateTimeKind.Utc || (dt.Kind == DateTimeKind.Unspecified && unspecifiedDateTimesAreUtc)
				? $"{dt:yyyy-MM-dd}T{dt:HH:mm:ss}Z"
				: $"{dt.ToUniversalTime():yyyy-MM-dd}T{dt.ToUniversalTime():HH:mm:ss}Z";
		}
		else if (value is DateTimeOffset dto)
		{
			return $"{dto.ToUniversalTime():yyyy-MM-dd}T{dto.ToUniversalTime():HH:mm:ss}Z";
		}

		return value.ToString() ?? string.Empty;
	}

	public static Filter Parse(string token) => Parse(token, null);

	public static Filter Parse(string token, IDictionary<string, string>? keyMappings)
	{
		var value2 = string.Empty;
		var propertyName = string.Empty;
		string? key;
		string? encodedValue;
		if (token.Contains(':', StringComparison.Ordinal))
		{
			key = token[..token.IndexOf(':')];
			encodedValue = token[(token.IndexOf(':') + 1)..];

			// lookup property name?
			if (keyMappings?.ContainsKey(key) == true)
			{
				propertyName = keyMappings[key];
			}
		}
		else
		{
			return new Filter();
		}

		string? value;
		FilterTypes filterType;
		if (encodedValue == "!(empty)")
		{
			value = string.Empty;
			filterType = FilterTypes.IsNotEmpty;
		}
		else if (encodedValue == "(empty)")
		{
			value = string.Empty;
			filterType = FilterTypes.IsEmpty;
		}
		else if (encodedValue == "!(null)")
		{
			value = string.Empty;
			filterType = FilterTypes.IsNotNull;
		}
		else if (encodedValue == "(null)")
		{
			value = string.Empty;
			filterType = FilterTypes.IsNull;
		}
		else if (encodedValue.StartsWith("!in(", StringComparison.OrdinalIgnoreCase) && encodedValue.EndsWith(')') && encodedValue.Length > 3)
		{
			value = Fix(encodedValue[4..^1]);
			filterType = FilterTypes.NotIn;
		}
		else if (encodedValue.StartsWith("in(", StringComparison.OrdinalIgnoreCase) && encodedValue.EndsWith(')') && encodedValue.Length > 3)
		{
			value = Fix(encodedValue[3..^1]);
			filterType = FilterTypes.In;
		}
		else if (encodedValue.StartsWith("!*", StringComparison.Ordinal) && encodedValue.EndsWith('*') && encodedValue.Length > 2)
		{
			value = Fix(encodedValue[2..^1]);
			filterType = FilterTypes.DoesNotContain;
		}
		else if (encodedValue.StartsWith('*') && encodedValue.EndsWith('*') && encodedValue.Length > 1)
		{
			value = Fix(encodedValue[1..^1]);
			filterType = FilterTypes.Contains;
		}
		else if (encodedValue.StartsWith('>') && encodedValue.EndsWith('<') && encodedValue.Contains('|', StringComparison.Ordinal) && encodedValue.Length > 1)
		{
			value = Fix(encodedValue[1..^1]);
			var idx = value.IndexOf('|');
			value2 = value[(idx + 1)..];
			value = value[..idx];
			filterType = FilterTypes.Range;
		}
		else if (encodedValue.EndsWith('*'))
		{
			value = Fix(encodedValue[..^1]);
			filterType = FilterTypes.StartsWith;
		}
		else if (encodedValue.StartsWith('*'))
		{
			value = Fix(encodedValue[1..]);
			filterType = FilterTypes.EndsWith;
		}
		else if (encodedValue.StartsWith('!'))
		{
			value = Fix(encodedValue[1..]);
			filterType = FilterTypes.DoesNotEqual;
		}
		else if (encodedValue.StartsWith(">=", StringComparison.Ordinal))
		{
			value = Fix(encodedValue[2..]);
			filterType = FilterTypes.GreaterThanOrEqual;
		}
		else if (encodedValue.StartsWith("<=", StringComparison.Ordinal))
		{
			value = Fix(encodedValue[2..]);
			filterType = FilterTypes.LessThanOrEqual;
		}
		else if (encodedValue.StartsWith('>'))
		{
			value = Fix(encodedValue[1..]);
			filterType = FilterTypes.GreaterThan;
		}
		else if (encodedValue.StartsWith('<'))
		{
			value = Fix(encodedValue[1..]);
			filterType = FilterTypes.LessThan;
		}
		else
		{
			value = Fix(encodedValue);
			filterType = FilterTypes.Equals;
		}

		// strip quotes
		//if (value.StartsWith("\"") && value.EndsWith("\""))
		//{
		//	value = value.Substring(1, value.Length - 2);
		//}

		return new Filter(filterType, key, value, value2) { PropertyName = propertyName };
	}

	private static string Fix(string value)
	{
		return value;
		// Don't do this, it's not needed and it breaks the parsing of dates
		// The formatting into a DateTime format is done in the Format method when applying the filter
		// This allows a more readable format to be used in the filter UI using the Format specified
		// as a component parameter

		//var trimmedValue = value.Trim('"');

		//if (DateTimeOffset.TryParseExact(trimmedValue, _formatsWithTimeZone, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out var parsedDateTimeOffset) ||
		//	DateTimeOffset.TryParseExact(trimmedValue, _formatsWithoutTimeZone, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal, out parsedDateTimeOffset))
		//{
		//	var returnValue = parsedDateTimeOffset.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
		//	return value == trimmedValue
		//		? returnValue
		//		: $"\"{returnValue}\"";
		//}

		//return value;
	}

	public static IEnumerable<Filter> ParseMany(string text, IDictionary<string, string>? keyMappings = null)
	{
		if (string.IsNullOrWhiteSpace(text) || !text.Contains(':'))
		{
			yield break;
		}

		bool token = false;
		bool quoted = false;
		bool hashed = false;
		var sb = new StringBuilder();

		// read next token
		foreach (var ch in text)
		{
			if (token)
			{
				if (char.IsWhiteSpace(ch) && !quoted && !hashed)
				{
					// not within quotes or hashes so end of next token
					yield return Parse(sb.ToString(), keyMappings);
					sb.Clear();
					token = false;
				}
				else
				{
					if (ch == '"')
					{
						quoted = !quoted;
					}
					else if (ch == '#' && !quoted)
					{
						hashed = !hashed;
					}

					sb.Append(ch);
				}
			}
			else
			{
				// consume leading white space
				if (char.IsWhiteSpace(ch))
				{
					continue;
				}

				token = true;
				sb.Append(ch);
			}
		}

		// possible end of string while still quoted
		if (token)
		{
			if (quoted)
			{
				sb.Append('"');
			}
			else if (hashed)
			{
				sb.Append('#');
			}

			yield return Parse(sb.ToString(), keyMappings);
		}
	}

	public static bool IsDateTime(string? dateTimeString, out DateTime dateTime, out string formatFound, out DatePrecision datePrecision)
	{
		var dateTimeFormats = _formatsWithoutTimeZone.Concat(_formatsWithTimeZone).Concat(["yyyy-MM-ddTHH:mm:ssZ"]).ToArray();

		foreach (var format in dateTimeFormats)
		{
			if (DateTime.TryParseExact(dateTimeString?.RemoveQuotes(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
			{
				if (format.Contains("fff"))
				{
					datePrecision = DatePrecision.Millisecond;
				}
				else if (format.Contains("ss"))
				{
					datePrecision = DatePrecision.Second;
				}
				else if (format.Contains('m'))
				{
					datePrecision = DatePrecision.Minute;
				}
				else if (format.Contains("HH"))
				{
					datePrecision = DatePrecision.Hour;
				}
				else if (format.Contains("dd"))
				{
					datePrecision = DatePrecision.Day;
				}
				else if (format.Contains("MM"))
				{
					datePrecision = DatePrecision.Month;
				}
				else if (format.Contains("yyyy"))
				{
					datePrecision = DatePrecision.Year;
				}
				else
				{
					// default to second
					datePrecision = DatePrecision.Second;
				}
				// format found
				formatFound = format;
				return true;
			}
		}
		// format not found
		datePrecision = DatePrecision.Second;
		formatFound = string.Empty;
		dateTime = DateTime.MinValue;
		return false;
	}

	#endregion
}

public enum DatePrecision
{
	Year,
	Month,
	Day,
	Hour,
	Minute,
	Second,
	Millisecond
}