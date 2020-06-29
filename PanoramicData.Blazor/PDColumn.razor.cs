using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public partial class PDColumn<TItem>
	{
		private string? _title;
		private Func<TItem, object>? _compiledFunc;

		public Func<TItem, object>? CompiledFunc
			=> _compiledFunc ??= Field?.Compile();

		/// <summary>
		/// The parent PDTable instance.
		/// </summary>
		[CascadingParameter(Name = "Table")]
		public PDTable<TItem> Table { get; set; } = null!;

		/// <summary>
		/// The Id - this should be unique per column in a table
		/// </summary>
		[Parameter]
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// Optional CSS class for the column cell.
		/// </summary>
		[Parameter] public string? TdClass { get; set; }

		/// <summary>
		/// Optional CSS class for the column header.
		/// </summary>
		[Parameter] public string? ThClass { get; set; }

		/// <summary>
		/// Optional text for the alt attribute of the cell.
		/// </summary>
		[Parameter] public string? HelpText { get; set; }

		/// <summary>
		/// Optional format string for displaying the field value.
		/// </summary>
		[Parameter] public string? Format { get; set; }

		/// <summary>
		/// If set will override the FieldExpression's name
		/// </summary>
		[Parameter]
		public string Title
		{
			get => _title ??= Field?.GetPropertyMemberInfo()?.Name ?? "";
			set { _title = value; }
		}

		/// <summary>
		/// A Linq expression that selects the field to be data bound to.
		/// </summary>
		[Parameter] public Expression<Func<TItem, object>>? Field { get; set; }

		/// <summary>
		/// Gets or sets an HTML template for the fields value.
		/// </summary>
		[Parameter] public RenderFragment<TItem>? Template { get; set; }

		protected override async Task OnInitializedAsync()
		{
			if (Table == null)
			{
				throw new InvalidOperationException($"Error initializing column {Id}. " +
					"Table reference is null which implies it did not initialize or that the column " +
					$"type '{typeof(TItem)}' does not match the table type.");
			}
			await Table.AddColumnAsync(this).ConfigureAwait(true);
		}

		public string Render(TItem rowData)
		{
			// If the item is null or the field is not set then nothing to output
			if (rowData is null)
			{
				return string.Empty;
			}

			// Get the value using the compiled and cached function
			var value = CompiledFunc?.Invoke(rowData);
			if (value == null)
			{
				return string.Empty;
			}

			// Return the string to be rendered
			return string.IsNullOrEmpty(Format)
				? value.ToString()
				: string.Format(CultureInfo.CurrentCulture, "{0:" + Format + "}", value);
		}

	}
}
