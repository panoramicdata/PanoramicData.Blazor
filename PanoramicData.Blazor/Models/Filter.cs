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

		public FilterTypes FilterType { get; set; }

		public string Key { get; set; } = string.Empty;

		public string Value { get; set; } = string.Empty;

		public KeyValuePair<string, object> Values { get; set; }

		public void Clear()
		{
			FilterType = FilterTypes.NoFilter;
			Value = string.Empty;
		}

		public bool IsValid => FilterType != FilterTypes.NoFilter && !string.IsNullOrWhiteSpace(Value);

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

				default:
					return string.Empty;
			}
		}

		public void UpdateFrom(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				FilterType = FilterTypes.NoFilter;
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
					// read until next un-quoted whitespace
					var filter = ParseMany(text.Substring(idx)).FirstOrDefault();
					if(filter is null)
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

		public static Filter Parse(string token)
		{
			var key = string.Empty;
			var value = string.Empty;
			var filterType = FilterTypes.NoFilter;
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

			if (encodedValue.StartsWith("!*") && encodedValue.EndsWith("*") && encodedValue.Length > 2)
			{
				value = encodedValue.Substring(2, encodedValue.Length - 3);
				filterType = FilterTypes.DoesNotContain;
			}
			else if (encodedValue.StartsWith("*") && encodedValue.EndsWith("*") && encodedValue.Length > 1)
			{
				value = encodedValue.Substring(1, encodedValue.Length - 2);
				filterType = FilterTypes.Contains;
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
			else
			{
				value = encodedValue;
				filterType = FilterTypes.Equals;
			}

			// strip quotes
			if(value.StartsWith("\"") && value.EndsWith("\""))
			{
				value = value.Substring(1, value.Length - 2);
			}

			return new Filter(filterType, key, value);
		}

		public static IEnumerable<Filter> ParseMany(string text)
		{
			if(string.IsNullOrWhiteSpace(text) || !text.Contains(':'))
			{
				yield break;
			}

			bool token = false;
			bool quoted = false;
			var  sb = new StringBuilder();

			// read next token
			foreach(var ch in text)
			{
				if(token)
				{
					if (char.IsWhiteSpace(ch) && !quoted)
					{
						// not within quotes so end of next token
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
			if(token)
			{
				if (quoted)
				{
					sb.Append('"');
				}
				yield return Parse(sb.ToString());
			}
		}

		#endregion
	}
}
