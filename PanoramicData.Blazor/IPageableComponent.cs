namespace PanoramicData.Blazor
{
	/// <summary>
	/// The IPageableComponent interface is implemented by components that display multiple
	/// items that may be grouped into pages.
	/// </summary>
	public interface IPageableComponent
    {
		/// <summary>
		/// Gets the current page size.
		/// </summary>
		int PageSize { get; }

		/// <summary>
		/// Instructs the component to show the specified page.
		/// </summary>
		/// <param name="page">The number of the page to be displayed.</param>
		void GoTo(int page);
    }
}
