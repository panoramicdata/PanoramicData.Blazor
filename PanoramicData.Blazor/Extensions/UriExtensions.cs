namespace PanoramicData.Blazor.Extensions;

public static class UriExtensions
{
	/// <summary>
	/// Builds a new Uri string based on the given Uri and the provided parameters. Existing query string parameters
	/// are preserved, and possible overridden.
	/// </summary>
	/// <param name="uri">Existing Uri optionally including query string parameters.</param>
	/// <param name="paramStringValues">A dictionary of new query string parameters.</param>
	/// <returns>A new Uri string containing the previous and new query string parameters.</returns>
	public static string GetQueryString(this Uri uri, Dictionary<string, StringValues> paramStringValues)
	{
		var query = QueryHelpers.ParseQuery(uri.Query);
		foreach (var entry in paramStringValues)
		{
			query[entry.Key] = entry.Value;
		}
		var items = query
			.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value))
			.ToList();
		var queryBuilder = new QueryBuilder(items);
		return uri.AbsolutePath + queryBuilder.ToQueryString();
	}
}
