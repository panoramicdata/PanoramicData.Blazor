using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Linq;
using Humanizer;
using System;

namespace PanoramicData.Blazor
{
	public partial class PDFormFooter<TItem> : IDisposable where TItem : class
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

		protected override void OnInitialized()
		{
			base.OnInitialized();
			if(Form != null)
			{
				// listen for error changes
				Form.ErrorsChanged += Form_ErrorsChanged;

				// create default buttons
				Buttons.Add(new ToolbarButton { Text = "Yes", CssClass = "btn-danger", IconCssClass = "fas fa-check",  ShiftRight = true });
				Buttons.Add(new ToolbarButton { Text = "No", CssClass="btn-secondary", IconCssClass = "fas fa-times" });
				Buttons.Add(new ToolbarButton { Text = "Delete", CssClass = "btn-danger", IconCssClass = "fas fa-trash-alt" });
				Buttons.Add(new ToolbarButton { Text = "Save", CssClass = "btn-primary", IconCssClass = "fas fa-save", ShiftRight = true });
				Buttons.Add(new ToolbarButton { Text = "Cancel", CssClass = "btn-secondary", IconCssClass = "fas fa-times" });
			}
		}

		private void SetVisibility(ToolbarItem? item, bool shown)
		{
			if(item != null)
			{
				item.IsVisible = shown;
			}
		}

		protected override void OnParametersSet()
		{
			// update state of default buttons
			if (Form != null)
			{
				SetVisibility(Buttons.Find(x => x.Key == "Yes"), Form.Mode == FormModes.Delete);
				SetVisibility(Buttons.Find(x => x.Key == "No"), Form.Mode == FormModes.Delete);
				SetVisibility(Buttons.Find(x => x.Key == "Delete"), Form.Mode == FormModes.Edit && ShowDelete);
				SetVisibility(Buttons.Find(x => x.Key == "Save"), Form.Mode == FormModes.Create || Form.Mode == FormModes.Edit);
				SetVisibility(Buttons.Find(x => x.Key == "Cancel"), Form.Mode == FormModes.Create || Form.Mode == FormModes.Edit);
			}
		}

		private void Form_ErrorsChanged(object sender, EventArgs e)
		{
			InvokeAsync(() =>
			{
				var saveButton = Buttons.Find(x => x.Key == "Save");
				if (saveButton != null)
				{
					var isValid = Form!.Errors.Count == 0;
					saveButton.IsEnabled = isValid;
					StateHasChanged();
				}
			}).ConfigureAwait(true);
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

		public void Dispose()
		{
			if(Form != null)
			{
				Form.ErrorsChanged -= Form_ErrorsChanged;
			}
		}
	}
}
