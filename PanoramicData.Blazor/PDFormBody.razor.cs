using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public partial class PDFormBody<TItem> where TItem : class
	{
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

		protected override void OnInitialized()
		{
			if (Table != null)
			{
				foreach(var column in Table.Columns)
				{
					Fields.Add(new FormField<TItem>
					{
						Field = column.Field,
						ReadOnlyInCreate = column.ReadOnlyInCreate,
						ReadOnlyInEdit = column.ReadOnlyInEdit,
						ShowInCreate = column.ShowInCreate,
						ShowInDelete = column.ShowInDelete,
						ShowInEdit = column.ShowInEdit,
						Template = column.Template,
						Title = column.Title
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
					Field = field.Field,
					ReadOnlyInCreate = field.ReadOnlyInCreate,
					ReadOnlyInEdit = field.ReadOnlyInEdit,
					ShowInCreate = field.ShowInCreate,
					ShowInDelete = field.ShowInDelete,
					ShowInEdit = field.ShowInEdit,
					Template = field.Template,
					Title = field.Title
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

		private bool IsShown(FormField<TItem> field) =>
			(field.ShowInCreate && Form?.Mode == FormModes.Create) ||
			(field.ShowInEdit && Form?.Mode == FormModes.Edit) ||
			(field.ShowInDelete && Form?.Mode == FormModes.Delete);

		private bool IsReadOnly(FormField<TItem> field) =>
			(field.ReadOnlyInCreate && Form?.Mode == FormModes.Create) ||
			(field.ReadOnlyInEdit && Form?.Mode == FormModes.Edit) ||
			Form?.Mode == FormModes.Delete;

		private string GetEditorType(FormField<TItem> field)
		{
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is PropertyInfo propInfo)
			{
				if(propInfo.PropertyType.IsEnum)
				{
					return "enum";
				}
				if (propInfo.PropertyType.FullName == "System.DateTime" || propInfo.PropertyType.FullName == "System.DateTimeOffset")
				{
					return "date";
				}
				if (propInfo.PropertyType.FullName == "System.Boolean")
				{
					return "checkbox";
				}
				if (propInfo.PropertyType.FullName.In("System.Byte", "System.SByte", "System.Int16", "System.Int32", "System.Int64", "System.UInt16",
						"System.UInt32", "System.UInt64", "System.Decimal", "System.Double", "System.Float"))
				{
					return "number";
				}
			}
			return "text";
		}

		private void OnInput(FormField<TItem> field, object value)
		{
			Form?.FieldChange(field, value);
		}

		private bool GetBoolValue(FormField<TItem> field)
		{
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (Form != null && memberInfo is PropertyInfo propInfo)
			{
				// check and return delta if set
				if(Form.Delta.ContainsKey(memberInfo.Name))
				{
					return (bool)Form.Delta[memberInfo.Name];
				}
				if(Form.Item is null)
				{
					return false;
				}
				return (bool)propInfo.GetValue(Form.Item);
			}
			return false;
		}

		private OptionInfo[] GetEnumValues(FormField<TItem> field)
		{
			var options = new List<OptionInfo>();
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is PropertyInfo propInfo)
			{
				var names = Enum.GetNames(propInfo.PropertyType);
				var values = Enum.GetValues(propInfo.PropertyType);
				for(var i = 0; i < values.Length; i++)
				{
					var displayName = propInfo.PropertyType.GetMember($"{names[i]}")
								   ?.First()
								   .GetCustomAttribute<DisplayAttribute>()
								   ?.Name ?? names[i];
					options.Add(new OptionInfo
					{
						Text = displayName,
						Value = values.GetValue(i).ToString(),
						IsSelected = field.GetRenderValue(Form?.Item)?.ToString() == values.GetValue(i).ToString()
					});
				}
			}
			return options.ToArray();
		}
	}
}
