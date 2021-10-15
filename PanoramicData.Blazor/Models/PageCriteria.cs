using System;

namespace PanoramicData.Blazor.Models
{
	public class PageCriteria
	{
		private uint _page;
		private uint _pageSize = 10;
		private uint _totalCount;

		/// <summary>
		/// Initializes a new instance of the PageCriteria class.
		/// </summary>
		public PageCriteria()
		{
			Page = 1;
		}

		/// <summary>
		/// Initializes a new instance of the PageCriteria class.
		/// </summary>
		/// <param name="page">Page number.</param>
		/// <param name="pageSize">Number of items per page.</param>
		/// <param name="totalCount">The total number of items.</param>
		public PageCriteria(uint page, uint pageSize = 10, uint totalCount = 0)
		{
			_page = page;
			_pageSize = pageSize;
			_totalCount = totalCount;
		}

		/// <summary>
		/// Event raised whenever the current page number changes.
		/// </summary>
		public event EventHandler? PageChanged;

		/// <summary>
		/// Event raised whenever the current page size changes.
		/// </summary>
		public event EventHandler? PageSizeChanged;

		/// <summary>
		/// Event raised whenever the total item count changes.
		/// </summary>
		public event EventHandler? TotalCountChanged;

		/// <summary>
		/// Gets or sets page number.
		/// </summary>
		public uint Page
		{
			get { return _page; }
			set
			{
				if (_page == value || value <= 0 || (value > 1 && value > PageCount))
				{
					return;
				}
				_page = value;
				OnPageChanged();
			}
		}

		/// <summary>
		/// Gets or sets the number of items per page.
		/// </summary>
		public uint PageSize
		{
			get { return _pageSize; }
			set
			{
				if (value == 0)
				{
					throw new ArgumentOutOfRangeException("PageSize must be greater than 0");
				}
				if (_pageSize == value)
				{
					return;
				}
				_pageSize = value;
				if (_page > PageCount)
				{
					Page = PageCount;
				}
				OnPageSizeChanged();
			}
		}

		/// <summary>
		/// Gets or sets the total number of items.
		/// </summary>
		public uint TotalCount
		{
			get { return _totalCount; }
			set
			{
				if (_totalCount == value)
				{
					return;
				}
				_totalCount = value;
				if (_totalCount == 0)
				{
					Page = 1;
				}
				else if (_page > PageCount)
				{
					Page = PageCount;
				}
				OnTotalCountChanged();
			}
		}

		/// <summary>
		/// Gets the total number of pages.
		/// </summary>
		public uint PageCount
		{
			get
			{
				return (_totalCount / _pageSize) + (uint)(_totalCount % _pageSize > 0 ? 1 : 0);
			}
		}

		/// <summary>
		/// Calculates the index of the first item of the current page.
		/// </summary>
		public uint PageRangeStart
		{
			get
			{
				return (_page - 1) * _pageSize + 1;
			}
		}

		/// <summary>
		/// Calculates the index of the last item of the current page.
		/// </summary>
		public uint PageRangeEnd
		{
			get
			{
				var last = (_page - 1) * _pageSize + _pageSize;
				return last > _totalCount ? _totalCount : last;
			}
		}

		/// <summary>
		/// Gets whether the current page is the first page.
		/// </summary>
		public bool IsFirstPage => _page == 1;

		/// <summary>
		/// Gets whether the current page is the last page.
		/// </summary>
		public bool IsLastPage => _page == PageCount;

		/// <summary>
		/// Gets the number of items before the current page.
		/// </summary>
		public uint PreviousItems => (_page - 1) * _pageSize;

		protected void OnPageChanged()
		{
			PageChanged?.Invoke(this, EventArgs.Empty);
		}

		protected void OnPageSizeChanged()
		{
			PageSizeChanged?.Invoke(this, EventArgs.Empty);
		}

		protected void OnTotalCountChanged()
		{
			TotalCountChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
