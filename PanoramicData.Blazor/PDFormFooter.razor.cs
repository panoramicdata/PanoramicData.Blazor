using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDFormFooter<TItem> where TItem : class
    {
		private List<ToolbarItem> Buttons { get; set; } = new List<ToolbarItem>();

		/// <summary>
		/// Sets a reference to the associated PDForm.
		/// </summary>
		[Parameter] public PDForm<TItem> Form { get; set; } = null!;

		/// <summary>
		/// Gets or sets the custom child content to be displayed in the header.
		/// </summary>
		[Parameter] public RenderFragment? ChildContent { get; set; }

		protected override void OnParametersSet()
		{
			// construct full button set
			if (Form is null)
			{

			}
			else
			{
				Buttons.Clear();
				if(Form.Mode == FormModes.Delete)
				{
					Buttons.Add(new ToolbarButton { Text = "Yes", CssClass = "btn-danger", ShiftRight = true });
					Buttons.Add(new ToolbarButton { Text = "No" });
				}
				else
				{
					Buttons.Add(new ToolbarButton { Text = "Save", CssClass = "btn-primary", ShiftRight = true });
					Buttons.Add(new ToolbarButton { Text = "Cancel" });
				}
			}
		}
	}
}
