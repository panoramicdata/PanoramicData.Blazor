using System.Collections.Generic;
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
				if (idx > -1)
				{
					// read until next un-quoted whitespace
					var sb = new StringBuilder();
					var quoted = false;
					var rht = text.Substring(idx + Key.Length + 1);
					if (rht.StartsWith("\""))
					{
						rht = rht.Substring(1);
						quoted = true;
					}
					foreach (var ch in rht)
					{
						if((quoted && ch == '"') || (!quoted && char.IsWhiteSpace(ch)))
						{
							break;
						}
						else
						{
							sb.Append(ch);
						}
					}
					var filter = Parse(sb.ToString());
					FilterType = filter.FilterType;
					Value = filter.Value;
				}
			}
		}

		public static Filter Parse(string token)
		{
			var key = string.Empty;
			var encodedValue = string.Empty;

			if (token.Contains(":"))
			{
				key = token.Substring(0, token.IndexOf(':'));
				encodedValue = token.Substring(token.IndexOf(':') + 1);
			}
			else
			{
				encodedValue = token;
			}

			if (encodedValue.StartsWith("!*") && encodedValue.EndsWith("*") && encodedValue.Length > 2)
			{
				return new Filter(FilterTypes.DoesNotContain, key, encodedValue.Substring(2, encodedValue.Length - 3));
			}
			if (encodedValue.StartsWith("*") && encodedValue.EndsWith("*") && encodedValue.Length > 1)
			{
				return new Filter(FilterTypes.Contains, key, encodedValue.Substring(1, encodedValue.Length - 2));
			}
			if (encodedValue.EndsWith("*"))
			{
				return new Filter(FilterTypes.StartsWith, key, encodedValue.Substring(0, encodedValue.Length - 1));
			}
			if (encodedValue.StartsWith("*"))
			{
				return new Filter(FilterTypes.EndsWith, key, encodedValue.Substring(1, encodedValue.Length - 1));
			}
			else if (encodedValue.StartsWith("!"))
			{
				return new Filter(FilterTypes.DoesNotEqual, key, encodedValue.Substring(1, encodedValue.Length - 1));
			}

			return new Filter(FilterTypes.Equals, key, encodedValue);
		}

		public static List<Filter> ParseMany(string text)
		{
			var filters = new List<Filter>();
			var tokens = text.Split(new char[] { ' ', '\t', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
			foreach(var token in tokens)
			{
				var filter = Filter.Parse(token);
				if(filter != null)
				{
					filters.Add(filter);
				}
			}
			return filters;
		}
	}
}
