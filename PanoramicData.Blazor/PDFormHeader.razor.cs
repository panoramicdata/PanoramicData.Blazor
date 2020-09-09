using System;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDFormHeader<TItem> where TItem : class
    {
		/// <summary>
		/// Form the component belongs to.
		/// </summary>
		[CascadingParameter] public PDForm<TItem>? Form { get; set; }

		/// <summary>
		/// Gets or sets the custom child content to be displayed in the header.
		/// </summary>
		[Parameter] public RenderFragment? ChildContent { get; set; }

		/// <summary>
		/// provides a function that will get a short description of the item being edited.
		/// </summary>
		[Parameter] public Func<TItem, string>? ItemDescription { get; set; }

		private string GetClassDescription()
		{
			// look for Display attribute and use name if provided, else fall-back to using class name.
			return typeof(TItem).GetCustomAttribute<DisplayAttribute>()?.Name ?? typeof(TItem).Name;
		}

		private string GetItemDescription()
		{
			return Form?.Item is null || ItemDescription is null ? GetClassDescription() : ItemDescription(Form.Item);
		}
	}
}
