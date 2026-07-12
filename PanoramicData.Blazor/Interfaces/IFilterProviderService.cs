namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines a service that provides distinct filter values for a given field, used by filter components to populate pick-lists.
/// </summary>
/// <typeparam name="TItem">The type of item whose fields can be queried for distinct values.</typeparam>
public interface IFilterProviderService<TItem>
{
	/// <summary>
	/// Gets a dictionary mapping filter key strings to the corresponding property names on <typeparamref name="TItem"/>.
	/// </summary>
	IDictionary<string, string> KeyPropertyMappings { get; }

	/// <summary>
	/// Returns the set of distinct values for the specified field, subject to the current data request.
	/// </summary>
	/// <param name="request">The data request context including any active search and sort criteria.</param>
	/// <param name="field">A lambda expression selecting the field whose distinct values are required.</param>
	/// <returns>An array of distinct values for the field.</returns>
	Task<object[]> GetDistinctValuesAsync(DataRequest<TItem> request, Expression<Func<TItem, object>> field);
}
