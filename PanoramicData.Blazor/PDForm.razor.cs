using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using PanoramicData.Blazor.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace PanoramicData.Blazor
{
	public partial class PDForm<TItem> where TItem : class
	{
		/// <summary>
		/// Injected log service.
		/// </summary>
		[Inject] protected ILogger<PDForm<TItem>> Logger { get; set; } = new NullLogger<PDForm<TItem>>();

		/// <summary>
		/// Sets the mode the form is operating in.
		/// </summary>
		[Parameter] public FormModes Mode { get; set; }

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
		/// Gets or sets the item being created / edited / deleted.
		/// </summary>
		[Parameter] public TItem? Item { get; set; }

		/// <summary>
		/// Gets a full list of all columns.
		/// </summary>
		public List<PDField<TItem>> Fields { get; } = new List<PDField<TItem>>();

		/// <summary>
		/// Adds the given field to the list of available fields.
		/// </summary>
		/// <param name="field">The PDColumn to be added.</param>
		public async Task AddFieldAsync(PDField<TItem> field)
		{
			try
			{
				Fields.Add(field);
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

		private bool IsShown(PDField<TItem> field) =>
			(field.ShowInCreate && Mode == FormModes.Create) ||
			(field.ShowInEdit && Mode == FormModes.Edit) ||
			(field.ShowInDelete && Mode == FormModes.Delete);

		private bool IsReadOnly(PDField<TItem> field) =>
			(field.ReadOnlyInCreate && Mode == FormModes.Create) ||
			(field.ReadOnlyInEdit && Mode == FormModes.Edit) ||
			Mode == FormModes.Delete;

		private string GetEditorType(PDField<TItem> field)
		{
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is System.Reflection.PropertyInfo propInfo)
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

		private bool EvaluateAsBool(object? value)
		{
			if(value == null)
			{
				return false;
			}
			return string.Equals(value.ToString(), "true", StringComparison.OrdinalIgnoreCase);
		}

		private OptionInfo[] GetEnumValues(PDField<TItem> field)
		{
			var options = new List<OptionInfo>();
			var memberInfo = field.Field?.GetPropertyMemberInfo();
			if (memberInfo is System.Reflection.PropertyInfo propInfo)
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
						IsSelected = field.GetValue(Item)?.ToString() == values.GetValue(i).ToString()
					});
				}
			}
			return options.ToArray();
		}
	}
}
