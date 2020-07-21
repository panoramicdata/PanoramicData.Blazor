using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor
{
	/// <summary>
	/// The ISortableComponent interface is implemented by components that display multiple
	/// items that may be sorted.
	/// </summary>
	public interface ISortableComponent
    {
		/// <summary>
		/// Sort the displayed items.
		/// </summary>
		/// <param name="sortCriteria">Details of the sort operation to be performed.</param>
		Task SortAsync(SortCriteria sortCriteria);

		/// <summary>
		/// Callback fired whenever the component changes the current sort criteria.
		/// </summary>
		EventCallback<SortCriteria> SortChanged { get; set; }
    }
}
