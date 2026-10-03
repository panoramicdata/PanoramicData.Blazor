using System;

namespace PanoramicData.Blazor.Models;

/// <summary>
/// A date and time parsed from a filter value by <see cref="Filter.ParseDateTime(string?)"/>.
/// </summary>
/// <param name="Value">The parsed date and time.</param>
/// <param name="Format">The format string that matched.</param>
/// <param name="Precision">The temporal precision the format carries.</param>
public sealed record ParsedDateTime(DateTime Value, string Format, DatePrecision Precision);
