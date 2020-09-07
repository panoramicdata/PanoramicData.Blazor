using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDFormFooter<TItem> where TItem : class
    {
		private List<ToolbarItem> Buttons { get; set; } = new List<ToolbarItem>();

		/// <summary>
		/// Form the component belongs to.
		/// </summary>
		[CascadingParameter] public PDForm<TItem>? Form { get; set; }

		/// <summary>
		/// Event raised whenever the user clicks on a button.
		/// </summary>
		[Parameter] public EventCallback<string> Click { get; set; }

		/// <summary>
		/// Should the Save button be shown?
		/// </summary>
		[Parameter] public bool ShowSave { get; set; } = true;

		/// <summary>
		/// Should the Cancel button be shown?
		/// </summary>
		[Parameter] public bool ShowCancel { get; set; } = true;

		/// <summary>
		/// Should the Delete button be shown (only applicable when in Edit mode)?
		/// </summary>
		[Parameter] public bool ShowDelete { get; set; } = true;

		protected override void OnParametersSet()
		{
			// construct full button set
			if (!(Form is null))
			{
				Buttons.Clear();
				if (Form.Mode == FormModes.Delete)
				{
					Buttons.Add(new ToolbarButton { Text = "Yes", CssClass = "btn-danger", ShiftRight = true });
					Buttons.Add(new ToolbarButton { Text = "No" });
				}
				else
				{
					if (Form.Mode == FormModes.Edit && ShowDelete)
					{
						Buttons.Add(new ToolbarButton { Text = "Delete", CssClass = "btn-danger" });
					}

					if (ShowSave)
					{
						Buttons.Add(new ToolbarButton { Text = "Save", CssClass = "btn-primary" });
					}
					if (ShowCancel)
					{
						Buttons.Add(new ToolbarButton { Text = "Cancel" });
					}
					if (Buttons.Count > 0)
					{
						var firstButton = Buttons.Find(x => x.Key != "Delete");
						if (!(firstButton is null))
						{
							firstButton.ShiftRight = true;
						}
					}
				}
			}
		}

		private async Task OnButtonClick(string key)
		{
			await Click.InvokeAsync(key).ConfigureAwait(true);

			if (Form != null)
			{
				if (key == "Delete")
				{
					Form.SetMode(FormModes.Delete);
				}
				else if (key == "Yes" && Form.DataProvider != null && Form.Item != null)
				{
					await Form.DeleteAsync().ConfigureAwait(true);
				}
				else if (key == "No")
				{
					Form.SetMode(FormModes.Edit);
				}
				else if (key == "Save" && Form.DataProvider != null && Form.Item != null)
				{
					await Form.SaveAsync().ConfigureAwait(true);
				}
			}
		}
	}
}
