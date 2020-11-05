using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	public partial class PDPager
    {
		private uint _page = 1;
		private uint _pageCount = 0;

		/// <summary>
		/// Sets the initial page count.
		/// </summary>
		[Parameter] public uint Page { get; set; } = 1;

		/// <summary>
		/// Sets the initial page count.
		/// </summary>
		[Parameter] public uint PageCount { get; set; } = 0;

		/// <summary>
		/// Callback invoked when the current page changes.
		/// </summary>
		[Parameter] public EventCallback<uint> PageChange { get; set; }

		/// <summary>
		/// Gets whether the current page is the first page.
		/// </summary>
		protected bool IsFirstPage => _page == 1;

		/// <summary>
		/// Gets whether the current page is the last page.
		/// </summary>
		protected bool IsLastPage => _page == _pageCount;

		protected override void OnInitialized()
		{
			_page = Page;
			_pageCount = PageCount;
		}

		public async Task MoveLast()
		{
			if (_page < _pageCount)
			{
				_page = _pageCount;
				await PageChange.InvokeAsync(_page).ConfigureAwait(true);
			}
		}

		public async Task MoveNext()
		{
			if (_page < _pageCount)
			{
				_page++;
				await PageChange.InvokeAsync(_page).ConfigureAwait(true);
			}
		}

		public async Task MovePrevious()
		{
			if (_page > 1)
			{
				_page--;
				await PageChange.InvokeAsync(_page).ConfigureAwait(true);
			}
		}

		public async Task MoveFirst()
		{
			if (_page > 1)
			{
				_page = 1;
				await PageChange.InvokeAsync(_page).ConfigureAwait(true);
			}
		}

		public async Task SetPageAsync(uint page)
		{
			if(page > 0 && page <= _pageCount && page != _page)
			{
				_page = page;
				await PageChange.InvokeAsync(_page).ConfigureAwait(true);
			}
		}

		public async Task SetPageCountAsync(uint count)
		{
			if (count < 1)
			{
				_pageCount = 0;
				if (_page != 1)
				{
					_page = 1;
					await PageChange.InvokeAsync(_page).ConfigureAwait(true);
				}
			}
			else
			{
				_pageCount = count;
				if(_page > _pageCount)
				{
					_page = _pageCount;
					await PageChange.InvokeAsync(_page).ConfigureAwait(true);
				}
			}
		}
	}
}
