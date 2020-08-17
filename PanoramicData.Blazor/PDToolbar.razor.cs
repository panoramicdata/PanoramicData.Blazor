using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
    public partial class PDToolbar
    {
		/// <summary>
		/// Child HTML content.
		/// </summary>
		[Parameter] public RenderFragment ChildContent { get; set; } = null!;

		/// <summary>
		/// Sets a list of application controlled toolbar items.
		/// </summary>
		[Parameter] public List<ToolbarItem>? Items { get; set; } = null;

		/// <summary>
		/// Event raised whenever the user clicks on a toolbar button.
		/// </summary>
		[Parameter] public EventCallback<string> ButtonClick { get; set; }

		private void OnButtonClick(string key)
		{
			ButtonClick.InvokeAsync(key);
		}
	}
}
