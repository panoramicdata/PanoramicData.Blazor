using Microsoft.JSInterop;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Interfaces;
using System;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Services
{
	public class NavigationCancelService : INavigationCancelService
	{
		private readonly IJSRuntime _jsRuntime;

		public NavigationCancelService(IJSRuntime jsRuntime)
		{
			_jsRuntime = jsRuntime;
		}

		/// <summary>
		/// Event raised before a navigation occurs.
		/// </summary>
		public event EventHandler<BeforeNavigateEventArgs> BeforeNavigate = default!;

		/// <summary>
		/// Determines whether the intended operation should proceed or be canceled.
		/// </summary>
		/// <param name="target">Optional data for intended operation. May be a target URL or operation name etc.</param>
		/// <returns>true if the operation should proceed otherwise false.</returns>
		public async Task<bool> ProceedAsync(string target = "")
		{
			// ask listening code if operation should be canceled
			var args = new BeforeNavigateEventArgs { Target = target };
			BeforeNavigate?.Invoke(this, args);
			if (args.Cancel)
			{
				// allow user option to override and perform operation regardless
				var proceed = await _jsRuntime.InvokeAsync<bool>("panoramicData.confirm", "Changes have been made, continue and lose those changes?").ConfigureAwait(true);
				if (proceed)
				{
					// remove all unload listeners
					await _jsRuntime.InvokeVoidAsync("panoramicData.removeUnloadListener").ConfigureAwait(true);
				}
				return proceed;
			}
			return true;
		}
	}
}
