using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public partial class PDField<TItem> where TItem : class
    {
		private string? _title;
		private Func<TItem, object>? _compiledFieldFunc;
		private Func<TItem, object>? CompiledFieldFunc => _compiledFieldFunc ??= Field?.Compile();

		/// <summary>
		/// The parent PDForm instance.
		/// </summary>
		[CascadingParameter(Name = "Form")]
		public PDForm<TItem> Form { get; set; } = null!;

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
			get => _title ??= Field?.GetPropertyMemberInfo()?.Name ?? "";
			set { _title = value; }
		}

		/// <summary>
		/// Gets or sets whether this field is visible when the form mode is Edit.
		/// </summary>
		[Parameter] public bool ShowInEdit { get; set; } = true;

		/// <summary>
		/// Gets or sets whether this field is visible when the form mode is Create.
		/// </summary>
		[Parameter] public bool ShowInCreate { get; set; } = true;

		/// <summary>
		/// Gets or sets whether this field is visible when the form mode is Delete.
		/// </summary>
		[Parameter] public bool ShowInDelete { get; set; } = false;

		/// <summary>
		/// Gets or sets whether this field is read-only when in Edit mode.
		/// </summary>
		[Parameter] public bool ReadOnlyInEdit { get; set; }

		/// <summary>
		/// Gets or sets whether this field is read-only when in Create mode.
		/// </summary>
		[Parameter] public bool ReadOnlyInCreate { get; set; }

		/// <summary>
		/// Gets or sets an HTML template for the fields editor.
		/// </summary>
		[Parameter] public RenderFragment<TItem>? Template { get; set; }

		protected override async Task OnInitializedAsync()
		{
			if (Form == null)
			{
				throw new InvalidOperationException("Error initializing field. " +
					"Form reference is null which implies it did not initialize or that the field " +
					$"type '{typeof(TItem)}' does not match the form type.");
			}
			await Form.AddFieldAsync(this).ConfigureAwait(true);
		}

		public object? GetValue(TItem? item)
		{
			if(item == null)
			{
				return null;
			}
			return CompiledFieldFunc?.Invoke(item);
		}
	}
}
