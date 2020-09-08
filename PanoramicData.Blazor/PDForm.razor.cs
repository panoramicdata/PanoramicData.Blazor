using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor
{
	public partial class PDForm<TItem> where TItem : class
    {
		/// <summary>
		/// Gets or sets the child content that the drop zone wraps.
		/// </summary>
		[Parameter] public RenderFragment? ChildContent { get; set; }

		/// <summary>
		/// Gets or sets the item being created / edited / deleted.
		/// </summary>
		[Parameter] public TItem? Item { get; set; }

		/// <summary>
		/// Gets or sets the IDataProviderService instance to use to save data.
		/// </summary>
		[Parameter] public IDataProviderService<TItem> DataProvider { get; set; } = null!;

		/// <summary>
		/// Event raised whenever the current item is successfully deleted.
		/// </summary>
		[Parameter] public EventCallback<TItem> Deleted { get; set; }

		/// <summary>
		/// Event raised when the current item has been successfully created.
		/// </summary>
		[Parameter] public EventCallback<TItem> Created { get; set; }

		/// <summary>
		/// Event raised when the current item has been successfully updated.
		/// </summary>
		[Parameter] public EventCallback<TItem> Updated { get; set; }

		/// <summary>
		/// Event raised whenever an error occurs.
		/// </summary>
		[Parameter] public EventCallback<string> Error { get; set; }

		/// <summary>
		/// Gets or sets the current form mode.
		/// </summary>
		public FormModes Mode { get; private set; }

		/// <summary>
		/// Gets or sets an object containing all modify
		/// </summary>
		public Dictionary<string, object> Delta { get; } = new Dictionary<string, object>();

		protected override void OnParametersSet()
		{
			if(Item == null)
			{
				Mode = FormModes.Hidden;
				Delta.Clear();
			}
		}

		/// <summary>
		/// Sets the current mode of the form.
		/// </summary>
		/// <param name="mode">The new mode for the form.</param>
		public void SetMode(FormModes mode)
		{
			if (mode != Mode)
			{
				Mode = mode;
				Delta.Clear();
				StateHasChanged();
			}
		}

		/// <summary>
		/// Send request to the DataProvider to delete the item.
		/// </summary>
		public async Task DeleteAsync()
		{
			if (DataProvider != null && Item != null)
			{
				var response = await DataProvider.DeleteAsync(Item, CancellationToken.None).ConfigureAwait(true);
				if (response.Success)
				{
					Mode = FormModes.Hidden;
					await Deleted.InvokeAsync(Item).ConfigureAwait(true);
				}
				else
				{
					await Error.InvokeAsync(response.ErrorMessage).ConfigureAwait(true);
				}
			}
		}

		/// <summary>
		/// Send request to the DataProvider to create or update the item.
		/// </summary>
		public async Task SaveAsync()
		{
			if (DataProvider != null && Item != null)
			{
				if (Mode == FormModes.Create)
				{
					var response = await DataProvider.CreateAsync(Item, CancellationToken.None).ConfigureAwait(true);
					if (response.Success)
					{
						Mode = FormModes.Hidden;
						await Created.InvokeAsync(Item).ConfigureAwait(true);
					}
					else
					{
						await Error.InvokeAsync(response.ErrorMessage).ConfigureAwait(true);
					}
				}
				else if(Mode == FormModes.Edit)
				{
					// convert delta from dictionary to anonymous object
					ExpandoObject eo = new ExpandoObject();
					var eoColl = (ICollection<KeyValuePair<string, object>>)eo;
					foreach(var kvp in Delta)
					{
						eoColl.Add(kvp);
					}

					var response = await DataProvider.UpdateAsync(Item, eo, CancellationToken.None).ConfigureAwait(true);
					if (response.Success)
					{
						Mode = FormModes.Hidden;
						await Updated.InvokeAsync(Item).ConfigureAwait(true);
					}
					else
					{
						await Error.InvokeAsync(response.ErrorMessage).ConfigureAwait(true);
					}
				}
			}
		}

		/// <summary>
		/// Indicates a fields value has been modified.
		/// </summary>
		/// <param name="field">The field that has been modified.</param>
		/// <param name="value">The new value for the field.</param>
		public void FieldChange(PDField<TItem> field, object value)
		{
			//TODO: re-run validation

			//
			if (field.Field != null)
			{
				var memberInfo = field.Field.GetPropertyMemberInfo();
				if (memberInfo != null)
				{
					// if create then apply change direct to item (as is new and can be discarded)
					if (Mode == FormModes.Create && memberInfo is PropertyInfo propInfo)
					{
						try
						{
							object typedValue = value.Cast(propInfo.PropertyType); // .GetValue(Item); // original value
							propInfo.SetValue(Item, typedValue);
						}
						catch (Exception ex)
						{
							Error.InvokeAsync($"Failed to update field {memberInfo.Name}: {ex.Message}");
						}
					}
					else if (Mode == FormModes.Edit)
					{
						// add / replace value on delta object
						Delta[memberInfo.Name] = value;
					}
				}
			}
		}
	}
}
