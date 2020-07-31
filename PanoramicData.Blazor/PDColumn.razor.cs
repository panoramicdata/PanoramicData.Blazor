using System;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public partial class PDColumn<TItem> where TItem: class
	{
		private string? _title;
		private Func<TItem, object>? _compiledFunc;
		private Func<TItem, object>? CompiledFunc => _compiledFunc ??= Field?.Compile();

		/// <summary>
		/// The parent PDTable instance.
		/// </summary>
		[CascadingParameter(Name = "Table")]
		public PDTable<TItem> Table { get; set; } = null!;

		/// <summary>
		/// The Id - this should be unique per column in a table
		/// </summary>
		[Parameter] public string Id { get; set; } = string.Empty;

		/// <summary>
		/// The data type of the columns field value.
		/// </summary>
		[Parameter] public Type? Type { get; set; }

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
		/// Gets or sets whether this column can be sorted.
		/// </summary>
		[Parameter] public bool Sortable { get; set; } = true;

		/// <summary>
		/// Gets or sets the default sort direction for this column.
		/// </summary>
		[Parameter] public SortDirection DefaultSortDirection { get; set; }

		/// <summary>
		/// This sets whether something CAN be shown in the list, use DTTable ColumnsToDisplay to dynamically
		/// change which to display from those that CAN be shown in the list
		/// </summary>
		[Parameter] public bool ShowInList { get; set; } = true;

		/// <summary>
		/// Gets or sets whether this column is visible.
		/// </summary>
		[Parameter] public bool Visible { get; set; } = true;

		/// <summary>
		/// Gets or sets whether this column is editable.
		/// </summary>
		[Parameter] public bool Editable { get; set; } = true;

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

		/// <summary>
		/// Gets or sets the attributes of the underlying property.
		/// </summary>
		public PropertyInfo? PropertyInfo { get; set; }

		/// <summary>
		/// Gets or sets this column is currently being sorted on.
		/// </summary>
		public bool SortColumn { get; set; }

		/// <summary>
		/// Gets or sets the current sort direction of this column.
		/// </summary>
		public SortDirection SortDirection { get; set; }

		/// <summary>
		/// Gets the column value from the given TItem.
		/// </summary>
		/// <param name="item">The TItem for the current row.</param>
		/// <returns>The columns value.</returns>
		public object? GetValue(TItem item)
		{
			return CompiledFunc?.Invoke(item);
		}

		/// <summary>
		/// Renders the field value for this column and the given item.
		/// </summary>
		/// <param name="item">The current item.</param>
		/// <returns>The item fields value, formatted if the Format property is set.</returns>
		public string Render(TItem item)
		{
			// If the item is null or the field is not set then nothing to output
			if (item is null)
			{
				return string.Empty;
			}

			// Get the value using the compiled and cached function
			var value = CompiledFunc?.Invoke(item);
			if (value == null)
			{
				return string.Empty;
			}

			// Return the string to be rendered
			return string.IsNullOrEmpty(Format)
				? value.ToString()
				: string.Format(CultureInfo.CurrentCulture, "{0:" + Format + "}", value);
		}

		/// <summary>
		/// Returns the markup to represent the current sort state.
		/// </summary>
		public string SortIcon
		{
			get
			{
				const string Blank = "<i class=\"fas fa-sort-amount-up-alt fa-hidden\"></i>";
				if (SortColumn)
				{
					return SortDirection switch
					{
						SortDirection.Ascending => "<i class=\"fas fa-sort-amount-up-alt\"></i>",
						SortDirection.Descending => "<i class=\"fas fa-sort-amount-down\"></i>",
						_ => Blank
					};
				}
				return Blank;
			}
		}

		/// <summary>
		/// Updates this column to be the sorted on column and calculates direction.
		/// </summary>
		/// <param name="requestedSortDirection">Direction to sort by. if omitted then will use the DefaultSortOrder property if
		/// the column is not currently being sorted on, otherwise reverse the current sort direction.</param>
		public async Task SortByAsync(SortDirection? requestedSortDirection = null)
		{
			try
			{
				if (!Sortable)
				{
					return;
				}

				// If we were already sorting by this column
				// and we've not been requested to change to a particular direction then reverse the direction
				if (SortColumn && !requestedSortDirection.HasValue)
				{
					SortDirection = SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
				}
				else
				{
					// Unset all other columns
					Table.Columns.ForEach(c => c.SortColumn = false);
					SortColumn = true;
					if (requestedSortDirection.HasValue)
						SortDirection = requestedSortDirection.Value;
				}
			}
			catch (Exception ex)
			{
				await Table.HandleExceptionAsync(ex).ConfigureAwait(true);
			}
		}

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

		protected override void OnParametersSet()
		{
			// Validate that enough parameters have been set correctly
			if (Type == null)
			{
				Type = Field?.GetPropertyMemberInfo()?.GetMemberUnderlyingType();
			}
			PropertyInfo = typeof(TItem).GetProperties().SingleOrDefault(p => p.Name == Field?.GetPropertyMemberInfo()?.Name);
		}
	}
}