namespace PanoramicData.Blazor.Models;

/// <summary>
/// Tabular results that <see cref="PDStudio"/> shows in a grid rather than as HTML.
/// </summary>
public class PDStudioResultSet
{
	/// <summary>Gets the columns, in order.</summary>
	public IReadOnlyList<PDStudioResultColumn> Columns { get; init; } = [];

	/// <summary>Gets the rows, each holding one value per column; a null value is a database NULL.</summary>
	public IReadOnlyList<object?[]> Rows { get; init; } = [];

	/// <summary>Gets whether more rows were available than were returned.</summary>
	public bool IsTruncated { get; init; }

	/// <summary>Gets the most rows the source would return, when it applied a limit.</summary>
	public int? RowLimit { get; init; }

	/// <summary>Gets how long the code took to run, when known.</summary>
	public TimeSpan? Duration { get; init; }

	/// <summary>Gets the number of rows as text, such as "1 row" or "1,250 rows".</summary>
	public string RowCountText => Rows.Count == 1
		? "1 row"
		: string.Format(CultureInfo.InvariantCulture, "{0:N0} rows", Rows.Count);

	/// <summary>Gets how long the code took to run as text, such as "142 ms" or "3.4 s", or null when not known.</summary>
	public string? DurationText => Duration switch
	{
		null => null,
		{ TotalSeconds: < 1 } d => string.Format(CultureInfo.InvariantCulture, "{0:N0} ms", d.TotalMilliseconds),
		var d => string.Format(CultureInfo.InvariantCulture, "{0:N1} s", d.Value.TotalSeconds)
	};

	/// <summary>
	/// Gets the results as CSV, with a heading row; values are quoted when they need to be.
	/// </summary>
	public string ToCsv()
	{
		var builder = new StringBuilder();
		builder.AppendLine(string.Join(',', Columns.Select(c => CsvValue(c.Name))));
		foreach (var row in Rows)
		{
			builder.AppendLine(string.Join(',', row.Select(v => CsvValue(FormatValue(v)))));
		}

		return builder.ToString();
	}

	/// <summary>
	/// Formats a value as the grid shows it: dates as ISO 8601, numbers in the invariant culture, NULL as empty.
	/// </summary>
	public static string FormatValue(object? value) => value switch
	{
		null or DBNull => string.Empty,
		DateTime d => d.ToString(d.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
		DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
		IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString() ?? string.Empty
	};

	/// <summary>
	/// Determines whether a value is a number, which the grid aligns right.
	/// </summary>
	public static bool IsNumber(object? value)
		=> value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

	private static string CsvValue(string value)
		=> value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
