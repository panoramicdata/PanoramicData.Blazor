namespace PanoramicData.Blazor
{
	public class PageCriteria
    {
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
		public PageCriteria(int page, int pageSize = 10)
		{
			Page = page;
			PageSize = pageSize;
		}

		/// <summary>
		/// Gets or sets page number.
		/// </summary>
		public int Page { get; set; }

		/// <summary>
		/// Gets or sets the number of items per page.
		/// </summary>
		public int PageSize { get; set; } = 10;
	}
}
