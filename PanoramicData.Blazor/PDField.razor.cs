using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public partial class PDField<TItem> where TItem : class
    {
		private string? _title;

		/// <summary>
		/// The parent PDForm instance.
		/// </summary>
		[CascadingParameter(Name = "FormBody")]
		public PDFormBody<TItem> FormBody { get; set; } = null!;

		/// <summary>
		/// A Linq expression that selects the field to be data bound to.
		/// </summary>
		[Parameter] public Expression<Func<TItem, object>>? Field { get; set; }

		/// <summary>
		/// If set will override the Field's name
		/// </summary>
		[Parameter]
		public string Title
		{
			get
			{
				if (_title == null)
				{
					var memberInfo = Field?.GetPropertyMemberInfo();
					if (memberInfo is PropertyInfo propInfo)
					{
						_title = propInfo.GetCustomAttribute<DisplayAttribute>()?.Name ?? propInfo.Name;
					}
					else
					{
						_title = memberInfo?.Name;
					}
				}
				return _title ?? "";
			}
			set { _title = value; }
		}

		/// <summary>
		/// Gets or sets a function that determines whether this field is visible when the form mode is Edit.
		/// </summary>
		[Parameter] public Func<TItem?, bool> ShowInEdit { get; set; } = new Func<TItem?, bool>((_) => true);

		/// <summary>
		/// Gets or sets a function that determines whether this field is visible when the form mode is Create.
		/// </summary>
		[Parameter] public Func<TItem?, bool> ShowInCreate { get; set; } = new Func<TItem?, bool>((_) => true);

		/// <summary>
		/// Gets or sets a function that determines whether this field is visible when the form mode is Create.
		/// </summary>
		[Parameter] public Func<TItem?, bool> ShowInDelete { get; set; } = new Func<TItem?, bool>((_) => false);

		/// <summary>
		/// Gets or sets a function that determines whether this field is read-only when the form mode is Edit.
		/// </summary>
		[Parameter] public Func<TItem?, bool> ReadOnlyInEdit { get; set; } = new Func<TItem?, bool>((_) => false);

		/// <summary>
		/// Gets or sets a function that determines whether this field is read-only when the form mode is Create.
		/// </summary>
		[Parameter] public Func<TItem?, bool> ReadOnlyInCreate { get; set; } = new Func<TItem?, bool>((_) => false);

		/// <summary>
		/// Gets a function that returns available value choices.
		/// </summary>
		[Parameter] public Func<FormField<TItem>, TItem?, OptionInfo[]>? Options { get; set; }

		/// <summary>
		/// Gets whether this field contains passwords or other sensitive information.
		/// </summary>
		[Parameter] public bool IsPassword { get; set; }

		/// <summary>
		/// Gets or sets an HTML template for the fields editor.
		/// </summary>
		[Parameter] public RenderFragment<TItem>? Template { get; set; }

		protected override async Task OnInitializedAsync()
		{
			if (FormBody == null)
			{
				throw new InvalidOperationException("Error initializing field. " +
					"FormBody reference is null which implies it did not initialize or that the field " +
					$"type '{typeof(TItem)}' does not match the form type.");
			}
			await FormBody.AddFieldAsync(this).ConfigureAwait(true);
		}
	}
}
