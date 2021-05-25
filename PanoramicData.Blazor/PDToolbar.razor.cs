using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;

namespace PanoramicData.Blazor
{
	public partial class PDToolbar
	{
		/// <summary>
		/// Gets or sets the button sizes.
		/// </summary>
		[Parameter] public ButtonSizes ButtonSize { get; set; } = ButtonSizes.Medium;

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
		[Parameter] public EventCallback<KeyedEventArgs<MouseEventArgs>> ButtonClick { get; set; }

		/// <summary>
		/// Gets or sets additional CSS classes for the toolbar.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = "";

		private void OnButtonClick(KeyedEventArgs<MouseEventArgs> args)
		{
			ButtonClick.InvokeAsync(args);
		}
	}
}
