namespace PanoramicData.Blazor.Models;

public class Filter
{
	public Filter()
	{
	}

	public Filter(FilterTypes filterType, string key, string value)
	{
		FilterType = filterType;
		Key = key;
		Value = value;
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
		switch (FilterType)
		{
			case FilterTypes.Equals:
				return $"{Key}:{Value}";

			case FilterTypes.DoesNotEqual:
				return $"{Key}:!{Value}";

			case FilterTypes.StartsWith:
				return $"{Key}:{Value}*";

			case FilterTypes.EndsWith:
				return $"{Key}:*{Value}";

			case FilterTypes.Contains:
				return $"{Key}:*{Value}*";

			case FilterTypes.DoesNotContain:
				return $"{Key}:!*{Value}*";

			case FilterTypes.In:
				return $"{Key}:In({Value})";

			case FilterTypes.GreaterThan:
				return $"{Key}:>{Value}";

			case FilterTypes.GreaterThanOrEqual:
				return $"{Key}:>={Value}";

			case FilterTypes.LessThan:
				return $"{Key}:<{Value}";

			case FilterTypes.LessThanOrEqual:
				return $"{Key}:<={Value}";

			case FilterTypes.Range:
				return $"{Key}:>{Value}|{Value2}<";

			case FilterTypes.IsNull:
				return $"{Key}:(null)";

			case FilterTypes.IsNotNull:
				return $"{Key}:!(null)";

			case FilterTypes.IsEmpty:
				return $"{Key}:(empty)";

			case FilterTypes.IsNotEmpty:
				return $"{Key}:!(empty)";

			default:
				return string.Empty;
		}
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
			var idx = text.IndexOf($"{Key}:");
			if (idx == -1)
			{
				Clear();
			}
			else
			{
				// read until next unquoted whitespace
				var filter = ParseMany(text.Substring(idx)).FirstOrDefault();
				if (filter is null)
				{
					Clear();
				}
				else
				{
					FilterType = filter.FilterType;
					Value = filter.Value;
				}
			}
		}
	}

	#region Class Members

	public static string Format(object value, bool unspecifiedDateTimesAreUtc = false)
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

		return value.ToString() ?? String.Empty;
	}

	public static Filter Parse(string token, IDictionary<string, string>? keyMappings = null)
	{
		var key = string.Empty;
		var value = string.Empty;
		var value2 = string.Empty;
		var propertyName = string.Empty;
		var filterType = FilterTypes.Equals;
		var encodedValue = string.Empty;

		if (token.Contains(":"))
		{
			key = token.Substring(0, token.IndexOf(':'));
			encodedValue = token.Substring(token.IndexOf(':') + 1);

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

		if (encodedValue == "!(empty)")
		{
			value = String.Empty;
			filterType = FilterTypes.IsNotEmpty;
		}
		else if (encodedValue == "(empty)")
		{
			value = String.Empty;
			filterType = FilterTypes.IsEmpty;
		}
		else if (encodedValue == "!(null)")
		{
			value = String.Empty;
			filterType = FilterTypes.IsNotNull;
		}
		else if (encodedValue == "(null)")
		{
			value = String.Empty;
			filterType = FilterTypes.IsNull;
		}
		else if (encodedValue.StartsWith("in(", System.StringComparison.OrdinalIgnoreCase) && encodedValue.EndsWith(")") && encodedValue.Length > 3)
		{
			value = encodedValue.Substring(3, encodedValue.Length - 4);
			filterType = FilterTypes.In;
		}
		else if (encodedValue.StartsWith("!*") && encodedValue.EndsWith("*") && encodedValue.Length > 2)
		{
			value = encodedValue.Substring(2, encodedValue.Length - 3);
			filterType = FilterTypes.DoesNotContain;
		}
		else if (encodedValue.StartsWith("*") && encodedValue.EndsWith("*") && encodedValue.Length > 1)
		{
			value = encodedValue.Substring(1, encodedValue.Length - 2);
			filterType = FilterTypes.Contains;
		}
		else if (encodedValue.StartsWith(">") && encodedValue.EndsWith("<") && encodedValue.Contains("|") && encodedValue.Length > 1)
		{
			value = encodedValue.Substring(1, encodedValue.Length - 2);
			var idx = value.IndexOf("|");
			value2 = value.Substring(idx + 1);
			value = value.Substring(0, idx);
			filterType = FilterTypes.Range;
		}
		else if (encodedValue.EndsWith("*"))
		{
			value = encodedValue.Substring(0, encodedValue.Length - 1);
			filterType = FilterTypes.StartsWith;
		}
		else if (encodedValue.StartsWith("*"))
		{
			value = encodedValue.Substring(1, encodedValue.Length - 1);
			filterType = FilterTypes.EndsWith;
		}
		else if (encodedValue.StartsWith("!"))
		{
			value = encodedValue.Substring(1, encodedValue.Length - 1);
			filterType = FilterTypes.DoesNotEqual;
		}
		else if (encodedValue.StartsWith(">="))
		{
			value = encodedValue.Substring(2, encodedValue.Length - 2);
			filterType = FilterTypes.GreaterThanOrEqual;
		}
		else if (encodedValue.StartsWith("<="))
		{
			value = encodedValue.Substring(2, encodedValue.Length - 2);
			filterType = FilterTypes.LessThanOrEqual;
		}
		else if (encodedValue.StartsWith(">"))
		{
			value = encodedValue.Substring(1, encodedValue.Length - 1);
			filterType = FilterTypes.GreaterThan;
		}
		else if (encodedValue.StartsWith("<"))
		{
			value = encodedValue.Substring(1, encodedValue.Length - 1);
			filterType = FilterTypes.LessThan;
		}
		else
		{
			value = encodedValue;
			filterType = FilterTypes.Equals;
		}

		// strip quotes
		//if (value.StartsWith("\"") && value.EndsWith("\""))
		//{
		//	value = value.Substring(1, value.Length - 2);
		//}

		return new Filter(filterType, key, value, value2) { PropertyName = propertyName };
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
				// consume leading whitespace
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

	#endregion
}
