namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a single query filter consisting of a key, a comparison operator (<see cref="FilterTypes"/>), and one or two values.
/// Filters can be serialised to and parsed from a compact string notation (e.g. <c>Name:*foo*</c>).
/// </summary>
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

		// US order (#197). No "K" variants here: a US date with an offset or Z is matched by the time zone
		// formats, and a "K" variant would match it first and report a different format.
		"MM'/'dd'/'yyyy HH:m",
		"MM'/'dd'/'yyyy HH:mm",
		"MM'/'dd'/'yyyy HH:mm:ss",
		"MM'/'dd'/'yyyy",

		"MM'/'dd'/'yyyy HH:m.fff",
		"MM'/'dd'/'yyyy HH:mm.fff",
		"MM'/'dd'/'yyyy HH:mm:ss.fff",

		// Hour precision (#197)
		"yyyy'-'MM'-'dd HH",
	];

	// Every format tried by IsDateTime, in order. Built once rather than on every call.
	private static readonly string[] _dateTimeFormats = [.. _formatsWithoutTimeZone, .. _formatsWithTimeZone, "yyyy-MM-ddTHH:mm:ssZ"];

	// Year and month precision (#197), tried last and only on request: see IsDateTime.
	private static readonly string[] _dateTimeFormatsWithYearAndMonth = [.. _dateTimeFormats, "yyyy'-'MM", "yyyy"];

	// How the encoded part of a token is recognised, tried in order: the first match decides the filter type.
	private static readonly FilterDecoder[] _decoders =
	[
		new(v => v == "!(empty)", _ => string.Empty, FilterTypes.IsNotEmpty),
		new(v => v == "(empty)", _ => string.Empty, FilterTypes.IsEmpty),
		new(v => v == "!(null)", _ => string.Empty, FilterTypes.IsNotNull),
		new(v => v == "(null)", _ => string.Empty, FilterTypes.IsNull),
		new(v => v.StartsWith("!in(", StringComparison.OrdinalIgnoreCase) && v.EndsWith(')') && v.Length > 3, v => v[4..^1], FilterTypes.NotIn),
		new(v => v.StartsWith("in(", StringComparison.OrdinalIgnoreCase) && v.EndsWith(')') && v.Length > 3, v => v[3..^1], FilterTypes.In),
		new(v => v.StartsWith("!*", StringComparison.Ordinal) && v.EndsWith('*') && v.Length > 2, v => v[2..^1], FilterTypes.DoesNotContain),
		new(v => v.StartsWith('*') && v.EndsWith('*') && v.Length > 1, v => v[1..^1], FilterTypes.Contains),
		new(v => v.StartsWith('>') && v.EndsWith('<') && v.Contains('|', StringComparison.Ordinal) && v.Length > 1, v => v[1..^1], FilterTypes.Range),
		new(v => v.EndsWith('*'), v => v[..^1], FilterTypes.StartsWith),
		new(v => v.StartsWith('*'), v => v[1..], FilterTypes.EndsWith),
		new(v => v.StartsWith('!'), v => v[1..], FilterTypes.DoesNotEqual),
		new(v => v.StartsWith(">=", StringComparison.Ordinal), v => v[2..], FilterTypes.GreaterThanOrEqual),
		new(v => v.StartsWith("<=", StringComparison.Ordinal), v => v[2..], FilterTypes.LessThanOrEqual),
		new(v => v.StartsWith('>'), v => v[1..], FilterTypes.GreaterThan),
		new(v => v.StartsWith('<'), v => v[1..], FilterTypes.LessThan),
		new(_ => true, v => v, FilterTypes.Equals)
	];

	/// <summary>Initializes a new, empty <see cref="Filter"/> with default values.</summary>
	public Filter()
	{
	}

	/// <summary>Initializes a new <see cref="Filter"/> with a string value.</summary>
	/// <param name="filterType">The comparison operator to apply.</param>
	/// <param name="key">The field key (column name) this filter targets.</param>
	/// <param name="value">The comparison value.</param>
	public Filter(FilterTypes filterType, string key, string value)
	{
		FilterType = filterType;
		Key = key;
		Value = value;
	}

	/// <summary>Initializes a new <see cref="Filter"/> converting the value to its string representation.</summary>
	/// <param name="filterType">The comparison operator to apply.</param>
	/// <param name="key">The field key (column name) this filter targets.</param>
	/// <param name="value">The comparison value; converted to string via <see cref="object.ToString"/>.</param>
	public Filter(FilterTypes filterType, string key, object value)
	{
		FilterType = filterType;
		Key = key;
		Value = value?.ToString() ?? string.Empty;
	}


	/// <summary>Initializes a new <see cref="Filter"/> with two values (used for range comparisons).</summary>
	/// <param name="filterType">The comparison operator to apply.</param>
	/// <param name="key">The field key (column name) this filter targets.</param>
	/// <param name="value">The lower bound or primary comparison value.</param>
	/// <param name="value2">The upper bound value used by <see cref="FilterTypes.Range"/>.</param>
	public Filter(FilterTypes filterType, string key, string value, string value2)
	{
		FilterType = filterType;
		Key = key;
		Value = value;
		Value2 = value2;
	}

	/// <summary>Gets or sets the comparison operator applied by this filter.</summary>
	public FilterTypes FilterType { get; set; }

	/// <summary>Gets or sets the field key (column name) this filter targets.</summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>Gets or sets the resolved property name on the model type, used when building LINQ predicates.</summary>
	public string PropertyName { get; set; } = string.Empty;

	/// <summary>Gets or sets the primary comparison value.</summary>
	public string Value { get; set; } = string.Empty;

	/// <summary>Gets or sets the secondary comparison value used by <see cref="FilterTypes.Range"/>.</summary>
	public string Value2 { get; set; } = string.Empty;

	/// <summary>Gets or sets an additional key/value pair of filter parameters.</summary>
	public KeyValuePair<string, object> Values { get; set; }

	/// <summary>Gets or sets whether <see cref="DateTimeKind.Unspecified"/> date/time values are treated as UTC when formatting filter values.</summary>
	public bool UnspecifiedDateTimesAreUtc { get; set; } = true;

	/// <summary>Resets the filter to an Equals comparison with empty values.</summary>
	public void Clear()
	{
		FilterType = FilterTypes.Equals;
		Value = string.Empty;
		Value2 = string.Empty;
	}

	/// <summary>Gets whether the filter has sufficient values set to be applied to a query.</summary>
	public bool IsValid => FilterType switch
	{
		FilterTypes.Range => !string.IsNullOrWhiteSpace(Value) && !string.IsNullOrWhiteSpace(Value2),
		FilterTypes.IsNull => true,
		FilterTypes.IsNotNull => true,
		FilterTypes.IsEmpty => true,
		FilterTypes.IsNotEmpty => true,
		_ => !string.IsNullOrWhiteSpace(Value)
	};

	/// <summary>
	/// Returns a compact string representation of this filter in the notation parsed by <see cref="Parse(string)"/>.
	/// Values containing whitespace are quoted to allow round-trip parsing via <see cref="ParseMany(string)"/>.
	/// </summary>
	public override string ToString()
	{
		// Quote values that contain whitespace so that ParseMany's whitespace tokeniser
		// does not split them into separate tokens. Parse() already strips these quotes.
		var v = QuoteIfNeeded(Value);
		var v2 = QuoteIfNeeded(Value2);
		return FilterType switch
		{
			FilterTypes.Equals => $"{Key}:{v}",
			FilterTypes.DoesNotEqual => $"{Key}:!{v}",
			FilterTypes.StartsWith => $"{Key}:{v}*",
			FilterTypes.EndsWith => $"{Key}:*{v}",
			FilterTypes.Contains => $"{Key}:*{v}*",
			FilterTypes.DoesNotContain => $"{Key}:!*{v}*",
			FilterTypes.In => $"{Key}:In({Value})",
			FilterTypes.NotIn => $"{Key}:!In({Value})",
			FilterTypes.GreaterThan => $"{Key}:>{v}",
			FilterTypes.GreaterThanOrEqual => $"{Key}:>={v}",
			FilterTypes.LessThan => $"{Key}:<{v}",
			FilterTypes.LessThanOrEqual => $"{Key}:<={v}",
			FilterTypes.Range => $"{Key}:>{v}|{v2}<",
			FilterTypes.IsNull => $"{Key}:(null)",
			FilterTypes.IsNotNull => $"{Key}:!(null)",
			FilterTypes.IsEmpty => $"{Key}:(empty)",
			FilterTypes.IsNotEmpty => $"{Key}:!(empty)",
			_ => string.Empty,
		};
	}

	private static string QuoteIfNeeded(string value)
	{
		// Already quoted - don't double-quote. This guards against callers that pre-quoted the value.
		if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
		{
			return value;
		}

		return value.Contains(' ') ? $"\"{value}\"" : value;
	}

	/// <summary>
	/// Updates this filter's type and values by parsing <paramref name="text"/> and finding the token whose key matches <see cref="Key"/>.
	/// Clears the filter if no matching token is found or <paramref name="text"/> is empty.
	/// </summary>
	/// <param name="text">A filter string in the notation produced by <see cref="ParseMany(string)"/>.</param>
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

	/// <summary>Formats <paramref name="value"/> into a filter-compatible string, treating unspecified <see cref="DateTime"/> values as local time.</summary>
	/// <param name="value">The value to format.</param>
	/// <returns>A string representation suitable for use as a filter value.</returns>
	public static string Format(object value) => Format(value, false);

	/// <summary>
	/// Reverse of Format() for enum types: translates a display name (from [Display(Name = ...)]) back
	/// to the actual enum member name needed by dynamic LINQ. If the value already matches a member
	/// name, or no matching display name is found, the original value is returned unchanged.
	/// </summary>
	public static string GetMemberName(Type enumType, string displayNameOrMemberName)
	{
		foreach (var member in enumType.GetMembers(BindingFlags.Public | BindingFlags.Static))
		{
			var displayName = member.GetCustomAttribute<DisplayAttribute>()?.Name;
			if (displayName == displayNameOrMemberName)
			{
				return member.Name;
			}
		}

		return displayNameOrMemberName;
	}

	/// <summary>
	/// Formats <paramref name="value"/> into a filter-compatible UTC ISO-8601 string.
	/// Enum values are mapped through their <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/> name when present.
	/// </summary>
	/// <param name="value">The value to format.</param>
	/// <param name="unspecifiedDateTimesAreUtc">When true, <see cref="DateTimeKind.Unspecified"/> date/times are treated as UTC.</param>
	/// <returns>A formatted string suitable for use as a filter value.</returns>
	public static string Format(object value, bool unspecifiedDateTimesAreUtc) => value switch
	{
		null => "",
		DateTime dt => FormatDateTime(dt, unspecifiedDateTimesAreUtc),
		DateTimeOffset dto => $"{dto.ToUniversalTime():yyyy-MM-dd}T{dto.ToUniversalTime():HH:mm:ss}Z",
		Enum => GetEnumDisplayName(value) ?? value.ToString() ?? string.Empty,
		_ => value.ToString() ?? string.Empty
	};

	private static string FormatDateTime(DateTime dt, bool unspecifiedDateTimesAreUtc)
		=> dt.Kind == DateTimeKind.Utc || (dt.Kind == DateTimeKind.Unspecified && unspecifiedDateTimesAreUtc)
			? $"{dt:yyyy-MM-dd}T{dt:HH:mm:ss}Z"
			: $"{dt.ToUniversalTime():yyyy-MM-dd}T{dt.ToUniversalTime():HH:mm:ss}Z";

	private static string? GetEnumDisplayName(object value)
		=> value.GetType().GetMember($"{value}").FirstOrDefault()?.GetCustomAttribute<DisplayAttribute>()?.Name;

	/// <summary>Parses a single filter token (e.g. <c>Name:*foo*</c>) into a <see cref="Filter"/> instance.</summary>
	/// <param name="token">The token string to parse.</param>
	/// <returns>A <see cref="Filter"/> representing the parsed token, or an empty filter if the token is invalid.</returns>
	public static Filter Parse(string token) => Parse(token, null);

	/// <summary>Parses a single filter token with optional key-to-property-name mappings.</summary>
	/// <param name="token">The token string to parse.</param>
	/// <param name="keyMappings">Optional dictionary mapping filter keys to model property names.</param>
	/// <returns>A <see cref="Filter"/> representing the parsed token, or an empty filter if the token is invalid.</returns>
	public static Filter Parse(string token, IDictionary<string, string>? keyMappings)
	{
		var separator = token.IndexOf(':');
		if (separator < 0)
		{
			return new Filter();
		}

		var key = token[..separator];
		var (filterType, value, value2) = Decode(token[(separator + 1)..]);

		// strip quotes added by ToString() to protect multi-word values during tokenisation
		// skip for In/NotIn as quotes belong to individual pipe-separated items
		if (filterType is not (FilterTypes.In or FilterTypes.NotIn))
		{
			value = Unquote(value);
		}

		// lookup property name?
		var propertyName = keyMappings is not null && keyMappings.TryGetValue(key, out var mappedName) ? mappedName : string.Empty;
		return new Filter(filterType, key, value, Unquote(value2)) { PropertyName = propertyName };
	}

	/// <summary>
	/// Splits the encoded part of a token (after the colon) into its filter type and values. Values are returned
	/// as written: dates are not reformatted here, which would break their parsing and lose the more readable
	/// format a filter UI may show. That formatting is done by <see cref="Format(object, bool)"/> when applying a filter.
	/// </summary>
	private static (FilterTypes FilterType, string Value, string Value2) Decode(string encodedValue)
	{
		var decoder = _decoders.First(d => d.Matches(encodedValue));
		var value = decoder.Extract(encodedValue);
		if (decoder.FilterType != FilterTypes.Range)
		{
			return (decoder.FilterType, value, string.Empty);
		}

		var idx = value.IndexOf('|');
		return (FilterTypes.Range, value[..idx], value[(idx + 1)..]);
	}

	private static string Unquote(string value)
		=> value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"') ? value[1..^1] : value;

	/// <summary>
	/// Parses a whitespace-delimited string of filter tokens into a sequence of <see cref="Filter"/> instances.
	/// Tokens containing spaces must be quoted. Returns an empty sequence if <paramref name="text"/> is blank or contains no colon.
	/// </summary>
	/// <param name="text">A string containing one or more filter tokens.</param>
	/// <returns>A sequence of parsed <see cref="Filter"/> instances.</returns>
	public static IEnumerable<Filter> ParseMany(string text) => ParseMany(text, null);

	/// <summary>
	/// Parses a whitespace-delimited string of filter tokens into a sequence of <see cref="Filter"/> instances.
	/// Tokens containing spaces must be quoted. Returns an empty sequence if <paramref name="text"/> is blank or contains no colon.
	/// </summary>
	/// <param name="text">A string containing one or more filter tokens.</param>
	/// <param name="keyMappings">Optional dictionary mapping filter keys to model property names.</param>
	/// <returns>A sequence of parsed <see cref="Filter"/> instances.</returns>
	public static IEnumerable<Filter> ParseMany(string text, IDictionary<string, string>? keyMappings)
	{
		if (string.IsNullOrWhiteSpace(text) || !text.Contains(':'))
		{
			return [];
		}

		return Tokenize(text).Select(token => Parse(token, keyMappings));
	}

	private static IEnumerable<string> Tokenize(string text)
	{
		var tokenizer = new FilterTokenizer();
		foreach (var ch in text)
		{
			if (tokenizer.Accept(ch) is { } token)
			{
				yield return token;
			}
		}

		// possible end of string while still quoted
		if (tokenizer.Finish() is { } lastToken)
		{
			yield return lastToken;
		}
	}

	/// <summary>
	/// Attempts to parse <paramref name="dateTimeString"/> as a <see cref="DateTime"/> using a set of recognised formats.
	/// Also returns the detected precision level and the matched format string.
	/// </summary>
	/// <param name="dateTimeString">The string to parse.</param>
	/// <param name="dateTime">The parsed <see cref="DateTime"/> value, or <see cref="DateTime.MinValue"/> on failure.</param>
	/// <param name="formatFound">The format string that matched, or an empty string on failure.</param>
	/// <param name="datePrecision">The temporal precision of the parsed value.</param>
	/// <returns>True if the string was successfully parsed; otherwise false.</returns>
	public static bool IsDateTime(string? dateTimeString, out DateTime dateTime, out string formatFound, out DatePrecision datePrecision)
		=> IsDateTime(dateTimeString, false, out dateTime, out formatFound, out datePrecision);

	/// <summary>
	/// Attempts to parse <paramref name="dateTimeString"/> as a <see cref="DateTime"/> using a set of recognised formats,
	/// optionally also recognising a year on its own (<c>2026</c>) and a year and month (<c>2026-03</c>).
	/// Also returns the detected precision level and the matched format string.
	/// </summary>
	/// <remarks>
	/// The year and month formats are tried only after every other format, so they never change how any other
	/// string parses. They are opt-in because a bare four-digit number is also a valid value for a numeric or
	/// text field: pass true only when the value is known to be compared against a date.
	/// </remarks>
	/// <param name="dateTimeString">The string to parse.</param>
	/// <param name="includeYearAndMonthFormats">
	/// True to also recognise <c>yyyy</c> (<see cref="DatePrecision.Year"/>) and <c>yyyy-MM</c> (<see cref="DatePrecision.Month"/>).
	/// </param>
	/// <param name="dateTime">The parsed <see cref="DateTime"/> value, or <see cref="DateTime.MinValue"/> on failure.</param>
	/// <param name="formatFound">The format string that matched, or an empty string on failure.</param>
	/// <param name="datePrecision">The temporal precision of the parsed value.</param>
	/// <returns>True if the string was successfully parsed; otherwise false.</returns>
	internal static bool IsDateTime(string? dateTimeString, bool includeYearAndMonthFormats, out DateTime dateTime, out string formatFound, out DatePrecision datePrecision)
	{
		var value = dateTimeString?.RemoveQuotes();
		var dateTimeFormats = includeYearAndMonthFormats ? _dateTimeFormatsWithYearAndMonth : _dateTimeFormats;

		foreach (var format in dateTimeFormats)
		{
			if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
			{
				datePrecision = GetPrecision(format);
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

	private static DatePrecision GetPrecision(string format)
	{
		if (format.Contains("fff"))
		{
			return DatePrecision.Millisecond;
		}

		if (format.Contains("ss"))
		{
			return DatePrecision.Second;
		}

		if (format.Contains('m'))
		{
			return DatePrecision.Minute;
		}

		if (format.Contains("HH"))
		{
			return DatePrecision.Hour;
		}

		if (format.Contains("dd"))
		{
			return DatePrecision.Day;
		}

		return format.Contains("MM") ? DatePrecision.Month : DatePrecision.Year;
	}
	#endregion

	private sealed record FilterDecoder(Func<string, bool> Matches, Func<string, string> Extract, FilterTypes FilterType);

	/// <summary>
	/// Reads filter tokens a character at a time: a token ends at whitespace that is not within quotes or hashes.
	/// </summary>
	private sealed class FilterTokenizer
	{
		private readonly StringBuilder _token = new();
		private bool _quoted;
		private bool _hashed;

		/// <summary>Consumes the next character, returning the token it ends, if any.</summary>
		public string? Accept(char ch)
		{
			if (_token.Length == 0)
			{
				// consume leading white space
				if (!char.IsWhiteSpace(ch))
				{
					_token.Append(ch);
				}

				return null;
			}

			if (char.IsWhiteSpace(ch) && !_quoted && !_hashed)
			{
				return TakeToken();
			}

			TrackEnclosure(ch);
			_token.Append(ch);
			return null;
		}

		private void TrackEnclosure(char ch)
		{
			if (ch == '"')
			{
				_quoted = !_quoted;
			}
			else if (ch == '#' && !_quoted)
			{
				_hashed = !_hashed;
			}
		}

		/// <summary>Returns the token in progress at the end of the text, closing an open quote or hash, if any.</summary>
		public string? Finish()
		{
			if (_token.Length == 0)
			{
				return null;
			}

			if (_quoted)
			{
				_token.Append('"');
			}
			else if (_hashed)
			{
				_token.Append('#');
			}

			return TakeToken();
		}

		private string TakeToken()
		{
			var token = _token.ToString();
			_token.Clear();
			return token;
		}
	}
}

/// <summary>
/// Specifies the temporal precision of a parsed date/time filter value.
/// </summary>
public enum DatePrecision
{
	/// <summary>Year-only precision.</summary>
	Year,
	/// <summary>Month precision (year and month).</summary>
	Month,
	/// <summary>Day precision (date only).</summary>
	Day,
	/// <summary>Hour precision.</summary>
	Hour,
	/// <summary>Minute precision.</summary>
	Minute,
	/// <summary>Second precision.</summary>
	Second,
	/// <summary>Millisecond precision.</summary>
	Millisecond
}