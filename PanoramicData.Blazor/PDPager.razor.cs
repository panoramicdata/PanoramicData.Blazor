using Microsoft.AspNetCore.Components;
using System;

namespace PanoramicData.Blazor
{
	public partial class PDPager : IDisposable
	{
		/// <summary>
		/// Gets or sets the text to be displayed when no items are available.
		/// </summary>
		[Parameter] public string NoItemsText { get; set; } = "No items to display";

		/// <summary>
		/// Sets the initial page count.
		/// </summary>
		[Parameter] public PageCriteria PageCriteria { get; set; } = new PageCriteria(1, 10, 0);

		/// <summary>
		/// Gets or sets the possible page sizes offered to the user.
		/// </summary>
		[Parameter] public uint[] PageSizeChoices { get; set; } = new uint[] { 10, 25, 50, 100, 250, 500 };

		/// <summary>
		/// Determines whether the description of the current page items is displayed.
		/// </summary>
		[Parameter] public bool ShowPageDescription { get; set; } = true;

		/// <summary>
		/// Determines whether the page size choices are displayed.
		/// </summary>
		[Parameter] public bool ShowPageSizeChoices { get; set; } = true;

		protected override void OnInitialized()
		{
			PageCriteria.TotalCountChanged += PageCriteria_TotalCountChanged;
		}
		public void Dispose()
		{
			PageCriteria.TotalCountChanged -= PageCriteria_TotalCountChanged;
		}

		private void PageCriteria_TotalCountChanged(object sender, System.EventArgs e)
		{
			StateHasChanged();
		}

		public void MoveLast()
		{
			PageCriteria.Page = PageCriteria.PageCount;
		}

		public void MoveNext()
		{
			PageCriteria.Page++;
		}

		public void MovePrevious()
		{
			PageCriteria.Page--;
		}

		public void MoveFirst()
		{
			PageCriteria.Page = 1;
		}
	}
}
