using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDFormBody<TItem> where TItem : class
	{
		/// <summary>
		/// Injected javascript interop object.
		/// </summary>
		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// Injected log service.
		/// </summary>
		[Inject] protected ILogger<PDForm<TItem>> Logger { get; set; } = new NullLogger<PDForm<TItem>>();

		/// <summary>
		/// Form the component belongs to.
		/// </summary>
		[CascadingParameter] public PDForm<TItem>? Form { get; set; }

		/// <summary>
		/// Gets or sets a linked PDTable instance that can be used to provide field definitions.
		/// </summary>
		[Parameter] public PDTable<TItem>? Table { get; set; }

		/// <summary>
		/// Child HTML content.
		/// </summary>
		[Parameter] public RenderFragment ChildContent { get; set; } = null!;

		/// <summary>
		/// Gets or sets a delegate to be called if an exception occurs.
		/// </summary>
		[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

		/// <summary>
		/// Gets or sets the width, in Pixels, of the Title box.
		/// </summary>
		[Parameter] public int TitleWidth { get; set; } = 200;

		/// <summary>
		/// Gets a full list of all fields.
		/// </summary>
		public List<FormField<TItem>> Fields { get; } = new List<FormField<TItem>>();

		/// <summary>
		/// Gets or sets a delegate to be called for each field validated.
		/// </summary>
		[Parameter] public EventCallback<CustomValidateArgs<TItem>> CustomValidate { get; set; }

		protected override void OnInitialized()
		{
			if (Table != null)
			{
				foreach (var column in Table.Columns)
				{
					Fields.Add(new FormField<TItem>
					{
						Id = column.Id,
						Field = column.Field,
						ReadOnlyInCreate = column.ReadOnlyInCreate,
						ReadOnlyInEdit = column.ReadOnlyInEdit,
						ShowInCreate = column.ShowInCreate,
						ShowInDelete = column.ShowInDelete,
						ShowInEdit = column.ShowInEdit,
						EditTemplate = column.EditTemplate,
						MaxLength = column.MaxLength,
						Title = column.Title,
						Options = column.Options,
						IsPassword = column.IsPassword,
						IsTextArea = column.IsTextArea,
						TextAreaRows = column.TextAreaRows,
						ShowValidationResult = column.ShowValidationResult,
						Description = column.Description,
						HelpUrl = column.HelpUrl
					});
				}
			}
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
		/// Attempts to get the requested fields current or original value and cast to the required type.
		/// </summary>
		/// <param name="field">The field whose value is to be fetched.</param>
		/// <param name="updatedValue">Should the current / updated value be returned or the original value?</param>
		/// <returns>The current or original field value cat to the appropriate type.</returns>
		/// <remarks>Use this method for Struct types only, use GetFieldStringValue() for String fields.</remarks>
		public object? GetFieldValue(FormField<TItem> field, bool updatedValue = true)
		{
			// point to relevant TItem instance
			if (Form?.Item is null)
			{
				return null;
			}

			// if original value required simply return
			object? value;
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is PropertyInfo propInfo)
			{
				if (updatedValue && Form.Delta.ContainsKey(memberInfo.Name))
				{
					value = Form.Delta[memberInfo.Name];
				}
				else
				{
					value = propInfo.GetValue(Form.Item);
				}
			}
			else
			{
				value = field.CompiledFieldFunc?.Invoke(Form.Item);
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
			if (Form?.Item is null)
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
			if (Form?.Item is null)
			{
				return string.Empty;
			}

			// if original value required simply return
			object? value;
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is PropertyInfo propInfo)
			{
				if (updatedValue && Form.Delta.ContainsKey(memberInfo.Name))
				{
					value = Form.Delta[memberInfo.Name];
				}
				else
				{
					value = propInfo.GetValue(Form.Item);
				}
			}
			else
			{
				value = field.CompiledFieldFunc?.Invoke(Form.Item);
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
			if (Form?.Item != null && field.Field != null)
			{
				var memberInfo = field.Field.GetPropertyMemberInfo();
				if (memberInfo != null && memberInfo is PropertyInfo propInfo)
				{
					try
					{
						// cast value
						object typedValue = value.Cast(propInfo.PropertyType);

						// run standard data annotation validation
						if (Form.Item != null)
						{
							var results = new List<ValidationResult>();
							var context = new ValidationContext(Form.Item)
							{
								MemberName = memberInfo.Name
							};
							if (Validator.TryValidateProperty(typedValue, context, results))
							{
								Form.ClearErrors(memberInfo.Name);
							}
							else
							{
								Form.SetFieldErrors(memberInfo.Name, results.Select(x => x.ErrorMessage).ToArray());
							}
						}

						// add / replace value on delta object
						Form.Delta[memberInfo.Name] = typedValue;

						// run custom validation
						if (Form.Item != null)
						{
							var args = new CustomValidateArgs<TItem>(field, GetItemWithUpdates());
							await CustomValidate.InvokeAsync(args).ConfigureAwait(true);
							foreach (var kvp in args.RemoveErrorMessages)
							{
								var entry = Form.Errors.FirstOrDefault(x => x.Key == kvp.Key && x.Value.Contains(kvp.Value));
								if (entry.Key != null)
								{
									Form.Errors[entry.Key].Remove(kvp.Value);
									if (Form.Errors[entry.Key].Count == 0)
									{
										Form.Errors.Remove(entry.Key);
									}
								}
							}
							foreach (var kvp in args.AddErrorMessages)
							{
								Form.SetFieldErrors(kvp.Key, kvp.Value);
							}
						}
					}
					catch (Exception ex)
					{
						Form?.SetFieldErrors(memberInfo.Name, ex.Message);
					}
				}
			}
		}

		private bool IsShown(FormField<TItem> field) =>
			(Form?.Mode == FormModes.Create && field.ShowInCreate(GetItemWithUpdates())) ||
			(Form?.Mode == FormModes.Edit && field.ShowInEdit(GetItemWithUpdates())) ||
			(Form?.Mode == FormModes.Delete && field.ShowInDelete(GetItemWithUpdates()));

		private TItem? GetItemWithUpdates()
		{
			if (Form?.Item is null)
			{
				return null;
			}
			var json = System.Text.Json.JsonSerializer.Serialize(Form.Item);
			var clone = System.Text.Json.JsonSerializer.Deserialize<TItem>(json);
			foreach (var kvp in Form.Delta)
			{
				var propInfo = clone.GetType().GetProperty(kvp.Key);
				propInfo?.SetValue(clone, kvp.Value);
			}
			return clone;
		}

		private bool IsReadOnly(FormField<TItem> field) =>
			(Form?.Mode == FormModes.Create && field.ReadOnlyInCreate(GetItemWithUpdates())) ||
			(Form?.Mode == FormModes.Edit && field.ReadOnlyInEdit(GetItemWithUpdates())) ||
			Form?.Mode == FormModes.Delete;

		private OptionInfo[] GetEnumValues(FormField<TItem> field)
		{
			var options = new List<OptionInfo>();
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is PropertyInfo propInfo)
			{
				var names = Enum.GetNames(propInfo.PropertyType);
				var values = Enum.GetValues(propInfo.PropertyType);
				for (var i = 0; i < values.Length; i++)
				{
					var displayName = propInfo.PropertyType.GetMember($"{names[i]}")
								   ?.First()
								   .GetCustomAttribute<DisplayAttribute>()
								   ?.Name ?? names[i];
					options.Add(new OptionInfo
					{
						Text = displayName,
						Value = values.GetValue(i),
						IsSelected = GetFieldStringValue(field) == values.GetValue(i).ToString()
					});
				}
			}
			return options.ToArray();
		}

		public string GetEditorClass(FormField<TItem> field)
		{
			return Form?.Errors.ContainsKey(field.GetName() ?? "") == true ? "invalid" : "";
		}

		private void OnHelpUrlClick(FormField<TItem> field)
		{
			JSRuntime.InvokeVoidAsync("panoramicData.openUrl", field.HelpUrl, "pd-help-page");
		}
	}
}