using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDPager
    {
		/// <summary>
		/// Current page number, when paging enabled.
		/// </summary>
		[ParameterAttribute]
		public int CurrentPage { get; set; } = 1;

		/// <summary>
		/// Total number of pages available.
		/// </summary>
		[ParameterAttribute]
		public int? PageCount { get; set; }

		/// <summary>
		/// Callback invoked when the current page changes.
		/// </summary>
		[Parameter] public EventCallback<int> OnPageChange { get; set; }

		/// <summary>
		/// Gets whether the current page is the first page.
		/// </summary>
		protected bool IsFirstPage => CurrentPage == 1;

		/// <summary>
		/// Gets whether the current page is the last page.
		/// </summary>
		protected bool IsLastPage => CurrentPage == PageCount;
	}
}
