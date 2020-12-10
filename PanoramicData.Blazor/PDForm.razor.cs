using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDForm<TItem> where TItem : class
	{
		private bool _showHelp;

		public event EventHandler? ErrorsChanged;

		/// <summary>
		/// Injected log service.
		/// </summary>
		[Inject] protected ILogger<PDForm<TItem>> Logger { get; set; } = new NullLogger<PDForm<TItem>>();

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
		/// Sets how help text is displayed.
		/// </summary>
		[Parameter] public HelpTextMode HelpTextMode { get; set; } = HelpTextMode.Toggle;

		/// <summary>
		/// Gets or sets a delegate to be called for each field validated.
		/// </summary>
		[Parameter] public EventCallback<CustomValidateArgs<TItem>> CustomValidate { get; set; }

		/// <summary>
		/// Gets or sets a delegate to be called if an exception occurs.
		/// </summary>
		[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

		/// <summary>
		/// Gets or sets the current form mode.
		/// </summary>
		public FormModes Mode { get; private set; }

		/// <summary>
		/// Gets a full list of all fields.
		/// </summary>
		public List<FormField<TItem>> Fields { get; } = new List<FormField<TItem>>();

		/// <summary>
		/// Gets a dictionary used to track uncommitted changes.
		/// </summary>
		public Dictionary<string, object> Delta { get; } = new Dictionary<string, object>();

		/// <summary>
		/// Gets a dictionary used to track validation errors.
		/// </summary>
		public Dictionary<string, List<string>> Errors { get; } = new Dictionary<string, List<string>>();

		/// <summary>
		/// Gets whether changes have been made.
		/// </summary>
		public bool HasChanges
		{
			get
			{
				return Delta.Count > 0;
			}
		}

		protected override void OnInitialized()
		{
			Mode = DefaultMode;
		}

		/// <summary>
		/// Adds the given field to the list of available fields.
		/// </summary>
		/// <param name="field">The PDColumn to be added.</param>
		public async Task AddFieldAsync(PDField<TItem> field)
		{
			try
			{
				Fields.Add(new FormField<TItem>
				{
					Id = field.Id,
					Field = field.Field,
					ReadOnlyInCreate = field.ReadOnlyInCreate,
					ReadOnlyInEdit = field.ReadOnlyInEdit,
					ShowInCreate = field.ShowInCreate,
					ShowInDelete = field.ShowInDelete,
					ShowInEdit = field.ShowInEdit,
					EditTemplate = field.EditTemplate,
					Title = field.Title,
					MaxLength = field.MaxLength,
					ShowValidationResult = field.ShowValidationResult,
					Options = field.Options,
					IsPassword = field.IsPassword,
					IsTextArea = field.IsTextArea,
					TextAreaRows = field.TextAreaRows,
					Description = field.Description,
					HelpUrl = field.HelpUrl
				});
				StateHasChanged();
			}
			catch (Exception ex)
			{
				await HandleExceptionAsync(ex).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Centralized method to process exceptions.
		/// </summary>
		/// <param name="ex">Exception that has been raised.</param>
		public async Task HandleExceptionAsync(Exception ex)
		{
			Logger.LogError(ex, ex.Message);
			await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
		}

		/// <summary>
		/// Sets the current item.
		/// </summary>
		/// <param name="item">The current item to be edited.</param>
		public void SetItem(TItem item)
		{
			Item = item;
		}

		/// <summary>
		/// Gets or sets whether help text should be displayed.
		/// </summary>
		public bool ShowHelp
		{
			get { return _showHelp; }
			set
			{
				if (value != _showHelp)
				{
					_showHelp = value;
					StateHasChanged();
				}
			}
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
			OnErrorsChanged(EventArgs.Empty);
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
				// validate all fields
				var errors = await ValidateFormAsync().ConfigureAwait(true);
				if (errors == 0)
				{
					if (Mode == FormModes.Create)
					{
						// apply delta to item
						var itemType = Item.GetType();
						foreach (var change in Delta)
						{
							try
							{
								var propInfo = itemType.GetProperty(change.Key);
								propInfo?.SetValue(Item, change.Value);
							}
							catch (Exception ex)
							{
								await Error.InvokeAsync($"Error applying delta to {change.Key}: {ex.Message}").ConfigureAwait(true);
								return;
							}
						}
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
					else if (Mode == FormModes.Edit)
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
		}

		/// <summary>
		/// Attempts to get the requested fields current or original value and cast to the required type.
		/// </summary>
		/// <param name="field">The field whose value is to be fetched.</param>
		/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
		/// <returns>The current or original field value cat to the appropriate type.</returns>
		/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>
		public object? GetFieldValue(FormField<TItem> field, bool updatedValue = true)
		{
			// point to relevant TItem instance
			if (Item is null)
			{
				return null;
			}

			// if original value required simply return
			object? value;
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is PropertyInfo propInfo)
			{
				if (updatedValue && Delta.ContainsKey(memberInfo.Name))
				{
					value = Delta[memberInfo.Name];
				}
				else
				{
					value = propInfo.GetValue(Item);
				}
			}
			else
			{
				value = field.CompiledFieldFunc?.Invoke(Item);
			}

			return value;
		}

		/// <summary>
		/// Attempts to get the requested fields current or original value and cast to the required type.
		/// </summary>
		/// <param name="field">The field whose value is to be fetched.</param>
		/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
		/// <returns>The current or original field value cat to the appropriate type.</returns>
		/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>
		public T GetFieldValue<T>(FormField<TItem> field, bool updatedValue = true) where T : struct
		{
			// point to relevant TItem instance
			if (Item is null)
			{
				return default;
			}
			object? value = GetFieldValue(field, updatedValue);
			if (value is null)
			{
				return default;
			}
			if (value is T t)
			{
				return t;
			}
			try
			{
				return (T)Convert.ChangeType(value, typeof(T));
			}
			catch
			{
				return default;
			}
		}

		/// <summary>
		/// Attempts to get the requested fields current or original value and cast to the required type.
		/// </summary>
		/// <param name="field">The field whose value is to be fetched.</param>
		/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
		/// <returns>The current or original field value cat to the appropriate type.</returns>
		/// <remarks>Use this method for String fields only, use GetFieldValue<T>() for Struct values.</remarks>
		public string GetFieldStringValue(FormField<TItem> field, bool updatedValue = true)
		{
			// point to relevant TItem instance
			if (Item is null)
			{
				return string.Empty;
			}

			// if original value required simply return
			object? value;
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is PropertyInfo propInfo)
			{
				if (updatedValue && Delta.ContainsKey(memberInfo.Name))
				{
					value = Delta[memberInfo.Name];
				}
				else
				{
					value = propInfo.GetValue(Item);
				}
			}
			else
			{
				value = field.CompiledFieldFunc?.Invoke(Item);
			}

			if (value is null)
			{
				return string.Empty;
			}
			if (value is DateTimeOffset dto)
			{
				// return simple date time string
				return dto.DateTime.ToString("yyyy-MM-dd");
			}
			if (value is DateTime dt)
			{
				// return date time string
				return dt.ToString("yyyy-MM-dd");
			}

			return value.ToString();
		}

		/// <summary>
		/// Updates the current value for the given field.
		/// </summary>
		/// <param name="field">The field to be updated.</param>
		/// <param name="value">The new value for the field.</param>
		/// <remarks>If valid, the new value is applied direct to the Item when in Create mode,
		/// otherwise tracked as a delta when in Edit mode.</remarks>
		public async Task SetFieldValueAsync(FormField<TItem> field, object value)
		{
			if (Item != null && field.Field != null)
			{
				var memberInfo = field.Field.GetPropertyMemberInfo();
				if (memberInfo != null && memberInfo is PropertyInfo propInfo)
				{
					// add / replace value on delta object
					object typedValue = value.Cast(propInfo.PropertyType);
					Delta[memberInfo.Name] = typedValue;

					// validate field
					await ValidateFieldAsync(field, value).ConfigureAwait(true);

					StateHasChanged();
				}
			}
		}

		/// <summary>
		/// Creates and returns a clone of the Item under edit with the current changes applied.
		/// </summary>
		/// <returns>A new TItem instance with changes applied.</returns>
		public TItem? GetItemWithUpdates()
		{
			if (Item is null)
			{
				return null;
			}
			var json = System.Text.Json.JsonSerializer.Serialize(Item);
			var clone = System.Text.Json.JsonSerializer.Deserialize<TItem>(json);
			foreach (var kvp in Delta)
			{
				var propInfo = clone.GetType().GetProperty(kvp.Key);
				propInfo?.SetValue(clone, kvp.Value);
			}
			return clone;
		}

		/// <summary>
		/// Validate all form fields.
		/// </summary>
		/// <returns>Count of all validation errors identified.</returns>
		public async Task<int> ValidateFormAsync()
		{
			Errors.Clear();
			foreach (var field in Fields)
			{
				if ((Mode == FormModes.Create && field.ShowInCreate(Item)) ||
					(Mode == FormModes.Edit && field.ShowInEdit(Item)))
				{
					await ValidateFieldAsync(field).ConfigureAwait(true);
				}
			}
			return Errors.Count;
		}

		/// <summary>
		/// Validates the given field and returns the typed value.
		/// </summary>
		/// <param name="field">The field to be validated.</param>
		/// <param name="value">The value to be validated, if omitted then will use the latest changed value for the given field.</param>
		/// <returns>Value converted to appropriate data type, otherwise null if problems casting.</returns>
		public async Task<object?> ValidateFieldAsync(FormField<TItem> field, object? value = null)
		{
			if (Item != null && field.Field != null)
			{
				var memberInfo = field.Field.GetPropertyMemberInfo();
				if (memberInfo != null && memberInfo is PropertyInfo propInfo)
				{
					try
					{
						// cast value
						object? typedValue = (value ?? GetFieldValue(field, true))?.Cast(propInfo.PropertyType);

						// run standard data annotation validation
						var results = new List<ValidationResult>();
						var context = new ValidationContext(Item)
						{
							MemberName = memberInfo.Name
						};
						if (Validator.TryValidateProperty(typedValue, context, results))
						{
							ClearErrors(memberInfo.Name);
						}
						else
						{
							SetFieldErrors(memberInfo.Name, results.Select(x => x.ErrorMessage).ToArray());
						}

						// run custom validation
						if (Item != null)
						{
							var args = new CustomValidateArgs<TItem>(field, GetItemWithUpdates());
							await CustomValidate.InvokeAsync(args).ConfigureAwait(true);
							foreach (var kvp in args.RemoveErrorMessages)
							{
								var entry = Errors.FirstOrDefault(x => x.Key == kvp.Key && x.Value.Contains(kvp.Value));
								if (entry.Key != null)
								{
									Errors[entry.Key].Remove(kvp.Value);
									if (Errors[entry.Key].Count == 0)
									{
										Errors.Remove(entry.Key);
									}
								}
							}
							foreach (var kvp in args.AddErrorMessages)
							{
								SetFieldErrors(kvp.Key, kvp.Value);
							}
						}

						return typedValue;
					}
					catch (Exception ex)
					{
						SetFieldErrors(memberInfo.Name, ex.Message);
					}
				}
			}
			return null;
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
			if (!Errors.ContainsKey(fieldName))
			{
				Errors.Add(fieldName, new List<string>());
			}
			// avoid duplicate messages
			foreach (var message in messages)
			{
				if (!Errors[fieldName].Contains(message))
				{
					Errors[fieldName].Add(message);
				}
			}
			OnErrorsChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Remove errors for the given field.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		public void ClearErrors(string fieldName)
		{
			Errors.Remove(fieldName);
			OnErrorsChanged(EventArgs.Empty);
		}

		protected virtual void OnErrorsChanged(EventArgs e)
		{
			ErrorsChanged?.Invoke(this, e);
		}
	}
}
