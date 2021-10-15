using System;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Models
{
	/// <summary>
	/// The FormFieldHelper class allows for a button to be shown for a given FormField that
	/// allows an application to provide a helper dialog when filling in the form field.
	/// </summary>
	public class FormFieldHelper<TItem> where TItem : class
	{
		/// <summary>
		/// Gets or sets the function to call upon user click.
		/// </summary>
		public Func<FormField<TItem>, FormFieldResult>? Click { get; set; }

		/// <summary>
		/// Gets or sets the function to call upon user click.
		/// </summary>
		public Func<FormField<TItem>, Task<FormFieldResult>>? ClickAsync { get; set; }

		/// <summary>
		/// Gets or sets the icon for the button.
		/// </summary>
		public string IconCssClass { get; set; } = string.Empty;

		/// <summary>
		/// Gets or set a tooltip for the button.
		/// </summary>
		public string ToolTip { get; set; } = string.Empty;
	}
}
