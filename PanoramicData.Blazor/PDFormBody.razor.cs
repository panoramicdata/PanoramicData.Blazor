using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace PanoramicData.Blazor
{
	public partial class PDFormBody<TItem> where TItem : class
	{
		/// <summary>
		/// Injected javascript interop object.
		/// </summary>
		[Inject] public IJSRuntime? JSRuntime { get; set; }

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
		/// Gets or sets the width, in Pixels, of the Title box.
		/// </summary>
		[Parameter] public int TitleWidth { get; set; } = 200;


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

		private bool IsShown(FormField<TItem> field) =>
			(Form?.Mode == FormModes.Create && field.ShowInCreate(Form?.GetItemWithUpdates())) ||
			(Form?.Mode == FormModes.Edit && field.ShowInEdit(Form?.GetItemWithUpdates())) ||
			(Form?.Mode == FormModes.Delete && field.ShowInDelete(Form?.GetItemWithUpdates()));

		private bool IsReadOnly(FormField<TItem> field) =>
			(Form?.Mode == FormModes.Create && field.ReadOnlyInCreate(Form?.GetItemWithUpdates())) ||
			(Form?.Mode == FormModes.Edit && field.ReadOnlyInEdit(Form?.GetItemWithUpdates())) ||
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

		private void OnHelpUrlClick(FormField<TItem> field)
		{
			JSRuntime.InvokeVoidAsync("panoramicData.openUrl", field.HelpUrl, "pd-help-page");
		}
	}
}