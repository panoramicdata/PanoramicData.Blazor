using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
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
		/// Sets the debounce wait period in milliseconds.
		/// </summary>
		[Parameter] public int DebounceWait { get; set; }

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
		/// Gets or sets whether the validation indicator should be shown for fields.
		/// </summary>
		[Parameter] public bool ShowValidationIndicator { get; set; } = true;

		/// <summary>
		/// Gets or sets the width, in Pixels, of the Title box.
		/// </summary>
		[Parameter] public int TitleWidth { get; set; } = 200;

		private Dictionary<string, object> GetNumericAttributes(FormField<TItem> field)
		{
			var dict = new Dictionary<string, object>();
			if (field.MaxValue.HasValue)
			{
				dict.Add("max", field.MaxValue.Value);
			}
			if (field.MinValue.HasValue)
			{
				dict.Add("min", field.MinValue.Value);
			}
			return dict;
		}

		protected override void OnInitialized()
		{
			if (Form != null && Table != null)
			{
				foreach (var column in Table.Columns)
				{
					Form.Fields.Add(new FormField<TItem>
					{
						Id = column.Id,
						Field = column.Field,
						ReadOnlyInCreate = column.ReadOnlyInCreate,
						ReadOnlyInEdit = column.ReadOnlyInEdit,
						ShowInCreate = column.ShowInCreate,
						ShowInDelete = column.ShowInDelete,
						ShowInEdit = column.ShowInEdit,
						EditTemplate = column.EditTemplate,
						Helper = column.Helper,
						MaxLength = column.MaxLength,
						Title = column.Title,
						Options = column.Options,
						IsPassword = column.IsPassword,
						IsSensitive = column.IsSensitive,
						IsTextArea = column.IsTextArea,
						TextAreaRows = column.TextAreaRows,
						ShowValidationResult = column.ShowValidationResult,
						Description = column.Description,
						HelpUrl = column.HelpUrl
					});
				}
			}
		}

		public bool IsShown(FormField<TItem> field, FormModes? mode = null)
		{
			if (mode == null)
			{
				mode = Form?.Mode;
			}
			return (mode == FormModes.Create && field.ShowInCreate(Form?.GetItemWithUpdates())) ||
				((mode == FormModes.Edit || mode == FormModes.ReadOnly) && field.ShowInEdit(Form?.GetItemWithUpdates())) ||
				(mode == FormModes.Delete && field.ShowInDelete(Form?.GetItemWithUpdates()));
		}

		public bool IsReadOnly(FormField<TItem> field) =>
			(Form?.Mode == FormModes.Create && field.ReadOnlyInCreate(Form?.GetItemWithUpdates())) ||
			(Form?.Mode == FormModes.Edit && field.ReadOnlyInEdit(Form?.GetItemWithUpdates())) ||
			Form?.Mode == FormModes.Delete ||
			Form?.Mode == FormModes.Cancel ||
			Form?.Mode == FormModes.ReadOnly;

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
						IsSelected = Form?.GetFieldStringValue(field) == values.GetValue(i).ToString()
					});
				}
			}
			return options.ToArray();
		}

		public string GetEditorClass(FormField<TItem> field)
		{
			return Form?.Errors.ContainsKey(field.GetName() ?? "") == true ? "invalid" : "";
		}

		private async Task OnHelperClick(FormField<TItem> field)
		{
			if (field != null && Form != null && (field?.Helper?.Click != null || field?.Helper?.ClickAsync != null))
			{
				FormFieldResult result = new FormFieldResult { Canceled = true };
				if (field.Helper?.Click != null)
				{
					result = field.Helper.Click(field);
				}
				else if (field.Helper?.ClickAsync != null)
				{
					result = await field.Helper.ClickAsync(field).ConfigureAwait(true);
				}
				if (!result.Canceled && result.NewValue != null)
				{
					await Form.SetFieldValueAsync(field, result.NewValue).ConfigureAwait(true);
				}
			}
		}

		private void OnHelpUrlClick(FormField<TItem> field)
		{
			JSRuntime.InvokeVoidAsync("panoramicData.openUrl", field.HelpUrl, "pd-help-page");
		}

		private string GetValidationCssClass(FormField<TItem> field)
		{
			var fieldName = field.GetName();
			if (IsReadOnly(field) || !field.ShowValidationResult)
			{
				return string.Empty;
			}
			else if (fieldName != null && Form?.Errors?.ContainsKey(fieldName) == true)
			{
				return "alert-danger";
			}
			else if (Form != null && field.GetIsRequired() && string.IsNullOrWhiteSpace(Form?.GetFieldValue(field)?.ToString()))
			{
				return "alert-warning";
			}
			else
			{
				return "alert-success";
			}
		}

		private string GetValidationIconCssClass(FormField<TItem> field)
		{
			var fieldName = field.GetName();
			if (IsReadOnly(field) || !field.ShowValidationResult)
			{
				return "pd-empty-icon";
			}
			else if (fieldName != null && Form?.Errors?.ContainsKey(fieldName) == true)
			{
				return "fas fa-exclamation-circle";
			}
			else if (Form != null && field.GetIsRequired() && string.IsNullOrWhiteSpace(Form?.GetFieldValue(field)?.ToString()))
			{
				return "fas fa-asterisk";
			}
			else
			{
				return "fas fa-check-circle";
			}
		}

		private async Task UpdateDateTimeValue(ChangeEventArgs args, FormField<TItem> field)
		{
			try
			{
				await Form!.SetFieldValueAsync(field, DateTime.SpecifyKind(Convert.ToDateTime(args.Value), DateTimeKind.Utc)).ConfigureAwait(true);
			}
			catch
			{
				Form!.SetFieldErrors(field.GetName() ?? "", "Invalid Date");
			}
		}

		private async Task UpdateDateTimeOffsetValue(ChangeEventArgs args, FormField<TItem> field)
		{
			try
			{
				await Form!.SetFieldValueAsync(field, DateTimeOffset.Parse(args.Value.ToString())).ConfigureAwait(true);
			}
			catch
			{
				Form!.SetFieldErrors(field.GetName() ?? "", "Invalid Date");
			}
		}

		private async Task UpdateValueViaCastAsync(ChangeEventArgs args, FormField<TItem> field)
		{
			try
			{
				await Form!.SetFieldValueAsync(field, Convert.ChangeType(args.Value, field.GetFieldType())).ConfigureAwait(true);
			}
			catch
			{
			}
		}
	}
}