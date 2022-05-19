using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;

namespace PanoramicData.Blazor.Models
{
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

		public string Value { get; set; } = string.Empty;

		public string Value2 { get; set; } = string.Empty;

		public KeyValuePair<string, object> Values { get; set; }

		public void Clear()
		{
			FilterType = FilterTypes.Equals;
			Value = string.Empty;
		}

		public bool IsValid => FilterType == FilterTypes.Range
			? !string.IsNullOrWhiteSpace(Value) && !string.IsNullOrWhiteSpace(Value2)
			: !string.IsNullOrWhiteSpace(Value);

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

		public static string Format(object value)
		{
			if (value is null)
			{
				return "";
			}
			if (value is DateTime dt)
			{
				return dt.TimeOfDay == TimeSpan.Zero
					? $"{dt:yyyy-MM-dd}"
					: $"#{dt:yyyy-MM-dd HH:mm:ss}#";
			}
			else if (value is DateTimeOffset dto)
			{
				return dto.UtcDateTime.TimeOfDay == TimeSpan.Zero
					? $"{dto.UtcDateTime:yyyy-MM-dd}"
					: $"#{dto.UtcDateTime:yyyy-MM-dd HH:mm:ss}#";
			}
			return value.ToString();
		}

		public static Filter Parse(string token)
		{
			var key = string.Empty;
			var value = string.Empty;
			var value2 = string.Empty;
			var filterType = FilterTypes.Equals;
			var encodedValue = string.Empty;

			if (token.Contains(":"))
			{
				key = token.Substring(0, token.IndexOf(':'));
				encodedValue = token.Substring(token.IndexOf(':') + 1);
			}
			else
			{
				return new Filter();
			}

			if (encodedValue.StartsWith("in(", System.StringComparison.OrdinalIgnoreCase) && encodedValue.EndsWith(")") && encodedValue.Length > 3)
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
			if (value.StartsWith("\"") && value.EndsWith("\""))
			{
				value = value.Substring(1, value.Length - 2);
			}

			return new Filter(filterType, key, value, value2);
		}

		public static IEnumerable<Filter> ParseMany(string text)
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
						yield return Parse(sb.ToString());
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
				yield return Parse(sb.ToString());
			}
		}

		#endregion
	}
}
