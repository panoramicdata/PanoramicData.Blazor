using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public class FormField<TItem> where TItem : class
	{
		private Func<TItem, object>? _compiledFieldFunc;

		internal Func<TItem, object>? CompiledFieldFunc => _compiledFieldFunc ??= Field?.Compile();

		/// <summary>
		/// Gets or sets a Linq expression that selects the field to be data bound to.
		/// </summary>
		public Expression<Func<TItem, object>>? Field { get; set; }

		/// <summary>
		/// Gets or sets the field title.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a function that determines whether this field is visible when the form mode is Edit.
		/// </summary>
		public Func<TItem?, bool> ShowInEdit { get; set; } = new Func<TItem?, bool>((_) => true);

		/// <summary>
		/// Gets or sets a function that determines whether this field is visible when the form mode is Create.
		/// </summary>
		public Func<TItem?, bool> ShowInCreate { get; set; } = new Func<TItem?, bool>((_) => true);

		/// <summary>
		/// Gets or sets a function that determines whether this field is visible when the form mode is Create.
		/// </summary>
		public Func<TItem?, bool> ShowInDelete { get; set; } = new Func<TItem?, bool>((_) => false);

		/// <summary>
		/// Gets or sets a function that determines whether this field is read-only when the form mode is Edit.
		/// </summary>
		public Func<TItem?, bool> ReadOnlyInEdit { get; set; } = new Func<TItem?, bool>((_) => false);

		/// <summary>
		/// Gets or sets a function that determines whether this field is read-only when the form mode is Create.
		/// </summary>
		public Func<TItem?, bool> ReadOnlyInCreate { get; set; } = new Func<TItem?, bool>((_) => false);

		/// <summary>
		/// Gets a function that returns available value choices.
		/// </summary>
		public Func<FormField<TItem>, TItem?, OptionInfo[]>? Options { get; set; }

		/// <summary>
		/// Gets or sets whether this field contains passwords or other sensitive information.
		/// </summary>
		public bool IsPassword { get; set; }

		/// <summary>
		/// Gets or sets an HTML template for the fields editor.
		/// </summary>
		public RenderFragment<TItem>? Template { get; set; }

		/// <summary>
		/// Returns the value to be rendered in the user interface.
		/// </summary>
		/// <param name="item">The current TItem instance where to obtain the current field value.</param>
		/// <returns>A value that can be rendered in the user interface.</returns>
		public object? GetRenderValue(TItem? item)
		{
			if (item == null)
			{
				return null;
			}
			var value = CompiledFieldFunc?.Invoke(item);
			if (value != null)
			{
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
			}
			return value;
		}

		/// <summary>
		/// Returns the field data type.
		/// </summary>
		/// <returns></returns>
		public Type? GetFieldType()
		{
			if(Field is null)
			{
				return null;
			}
			return Field?.GetPropertyMemberInfo()?.GetMemberUnderlyingType();
		}

		/// <summary>
		/// Simple function that returns true.
		/// </summary>
		public static Func<TItem?, bool> True => new Func<TItem?, bool>((_) => true);

		/// <summary>
		/// Simple function that returns false.
		/// </summary>
		public static Func<TItem?, bool> False => new Func<TItem?, bool>((_) => false);
	}
}
