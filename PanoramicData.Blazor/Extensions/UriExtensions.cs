using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Http.Extensions;

namespace PanoramicData.Blazor.Extensions
{
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

		/// <summary>
		/// Navigates to a new location base on existing Uri combined with the given query string parameters.
		/// </summary>
		/// <param name="navigationManager">NavigationManager instance containing current Uri.</param>
		/// <param name="paramStringValues">A dictionary of query string parameters to be applied.</param>
		public static void SetUri(this NavigationManager navigationManager, Dictionary<string, StringValues> paramStringValues)
		{
			var uri = new Uri(navigationManager.Uri);
			var newUriString = uri.GetQueryString(paramStringValues);
			navigationManager.NavigateTo(newUriString);
		}

		/// <summary>
		/// Navigates to a new location base on existing Uri combined with the given query string parameters.
		/// </summary>
		/// <param name="navigationManager">NavigationManager instance containing current Uri.</param>
		/// <param name="paramStringValues">A dictionary of query string parameters to be applied.</param>
		public static void SetUri(this NavigationManager navigationManager, Dictionary<string, object> paramStringValues)
		{
			var uri = new Uri(navigationManager.Uri);
			var newUriString = uri.GetQueryString(paramStringValues.ToDictionary(e => e.Key, e => new StringValues(e.Value.ToString())));
			navigationManager.NavigateTo(newUriString);
		}
	}
}
