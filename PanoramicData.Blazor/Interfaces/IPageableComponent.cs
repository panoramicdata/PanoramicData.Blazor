using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Interfaces
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
		//int PageSize { get; }

		/// <summary>
		/// Instructs the component to show a specified page.
		/// </summary>
		/// <param name="pageCriteria">Details of the page to be displayed.</param>
		Task PageAsync(PageCriteria pageCriteria);

		/// <summary>
		/// Callback fired whenever the component changes the current page details.
		/// </summary>
		EventCallback<PageCriteria> PageChanged { get; set; }
	}
}
