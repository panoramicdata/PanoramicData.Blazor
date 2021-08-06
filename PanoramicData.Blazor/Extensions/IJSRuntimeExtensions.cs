using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Extensions
{
	public static class IJSRuntimeExtensions
	{
		/// <summary>
		/// Updates the browsers current address without adding to the browser history.
		/// </summary>
		/// <param name="navigationManager">NavigationManager instance containing current Uri.</param>
		/// <param name="jsRuntime">Javascript Runtime service.</param>
		/// <param name="paramStringValues">A dictionary of query string parameters to be applied.</param>
		public static async Task UpdateUri(this IJSRuntime jsRuntime, Dictionary<string, object> paramStringValues)
		{
			var address = await jsRuntime.InvokeAsync<string>("panoramicData.getAddress").ConfigureAwait(true);
			var uri = new Uri(address);
			var newUriString = uri.GetQueryString(paramStringValues.ToDictionary(e => e.Key, e => new StringValues(e.Value.ToString())));
			await jsRuntime.InvokeVoidAsync("panoramicData.updateAddress", newUriString).ConfigureAwait(true);
		}
	}
}
