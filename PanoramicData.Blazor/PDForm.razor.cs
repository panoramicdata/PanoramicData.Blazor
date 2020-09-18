using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor
{
	public partial class PDForm<TItem> where TItem : class
    {
		public event EventHandler? ErrorsChanged;

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
		/// Sets the default mode of the form.
		/// </summary>
		[Parameter] public FormModes DefaultMode { get; set; }

		/// <summary>
		/// Gets or sets the current form mode.
		/// </summary>
		public FormModes Mode { get; private set; }

		/// <summary>
		/// Gets a dictionary used to track uncommitted changes.
		/// </summary>
		public Dictionary<string, object> Delta { get; } = new Dictionary<string, object>();

		/// <summary>
		/// Gets a dictionary used to track validation errors.
		/// </summary>
		public Dictionary<string, List<string>> Errors { get; } = new Dictionary<string, List<string>>();

		protected override void OnInitialized()
		{
			Mode = DefaultMode;
		}

		/// <summary>
		/// Sets the current mode of the form.
		/// </summary>
		/// <param name="mode">The new mode for the form.</param>
		public void SetMode(FormModes mode)
		{
			Mode = mode;
			Delta.Clear();
			Errors.Clear();
			StateHasChanged();
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
					var response = await DataProvider.UpdateAsync(Item, Delta, CancellationToken.None).ConfigureAwait(true);
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
		/// Gets whether the form is currently valid.
		/// </summary>
		public bool IsValid()
		{
			return Errors.Count == 0;
		}

		/// <summary>
		/// Adds one or more error messages for the given field.
		/// </summary>
		/// <param name="fieldName">Name of the field being validated.</param>
		/// <param name="messages">One or more error messages.</param>
		public void SetFieldErrors(string fieldName, params string[] messages)
		{
			if(!Errors.ContainsKey(fieldName))
			{
				Errors.Add(fieldName, new List<string>());
			}
			Errors[fieldName].AddRange(messages);
			OnErrorsChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Remove errors for the given field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		public void ClearErrors(string fieldName)
		{
			Errors.Remove(fieldName);
			OnErrorsChanged(new EventArgs());
		}

		protected virtual void OnErrorsChanged(EventArgs e)
		{
			ErrorsChanged?.Invoke(this, e);
		}
	}
}
