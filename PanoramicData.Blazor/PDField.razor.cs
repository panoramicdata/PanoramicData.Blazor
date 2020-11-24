using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

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
		/// The Id - this should be unique per column in a table
		/// </summary>
		[Parameter] public string Id { get; set; } = string.Empty;

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
		/// Gets or sets whether this field contains longer sections of text.
		/// </summary>
		[Parameter] public bool IsTextArea { get; set; }

		/// <summary>
		/// Gets or sets the number of rows of text displayed by default in a text area.,
		/// </summary>
		[Parameter] public int TextAreaRows { get; set; } = 4;

		/// <summary>
		/// Gets or sets the maximum length for entered text.
		/// </summary>
		[Parameter] public int? MaxLength { get; set; }

		/// <summary>
		/// Gets or sets whether the validation result should be shown when displayed.
		/// </summary>
		[Parameter] public bool ShowValidationResult { get; set; } = true;

		/// <summary>
		/// Gets or sets an HTML template for editing.
		/// </summary>
		[Parameter] public RenderFragment<TItem?>? EditTemplate { get; set; }

		/// <summary>
		/// Gets or sets an HTML template for the fields editor.
		/// </summary>
		[Parameter] public RenderFragment<TItem>? Template { get; set; }

		/// <summary>
		/// Gets or sets a short description of the fields purpose. Overrides DisplayAttribute description if set.
		/// </summary>
		[Parameter] public string? Description { get; set; }

		/// <summary>
		/// Gets or sets a URL to an external context sensitive help page.
		/// </summary>
		[Parameter] public string? HelpUrl { get; set; }

		protected override async Task OnInitializedAsync()
		{
			if (FormBody is null || FormBody.Form is null)
			{
				throw new InvalidOperationException("Error initializing field. " +
					"FormBody reference is null which implies it did not initialize or that the field " +
					$"type '{typeof(TItem)}' does not match the form type.");
			}
			await FormBody.Form.AddFieldAsync(this).ConfigureAwait(true);
		}
	}
}
