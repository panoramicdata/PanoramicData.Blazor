using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public class FormField<TItem> where TItem : class
	{
		private Func<TItem, object>? _compiledFieldFunc;
		private Func<TItem, object>? CompiledFieldFunc => _compiledFieldFunc ??= Field?.Compile();

		/// <summary>
		/// Gets or sets a Linq expression that selects the field to be data bound to.
		/// </summary>
		public Expression<Func<TItem, object>>? Field { get; set; }

		/// <summary>
		/// Gets or sets the field title.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets whether this field is visible when the form mode is Edit.
		/// </summary>
		public bool ShowInEdit { get; set; } = true;

		/// <summary>
		/// Gets or sets whether this field is visible when the form mode is Create.
		/// </summary>
		public bool ShowInCreate { get; set; } = true;

		/// <summary>
		/// Gets or sets whether this field is visible when the form mode is Delete.
		/// </summary>
		public bool ShowInDelete { get; set; } = false;

		/// <summary>
		/// Gets or sets whether this field is read-only when in Edit mode.
		/// </summary>
		public bool ReadOnlyInEdit { get; set; }

		/// <summary>
		/// Gets or sets whether this field is read-only when in Create mode.
		/// </summary>
		public bool ReadOnlyInCreate { get; set; }

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
	}
}
