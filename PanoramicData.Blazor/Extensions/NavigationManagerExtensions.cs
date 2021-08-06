using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PanoramicData.Blazor.Extensions
{
	public static class NavigationManagerExtensions
	{
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
