namespace PanoramicData.Blazor.Services;

/// <summary>
/// In-memory <see cref="IDataProviderService{TItem}"/> implementation backed by a mutable <see cref="List{T}"/>.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public class ListDataProviderService<TItem> : IDataProviderService<TItem>
{

	/// <summary>
	/// Initializes an empty list-backed provider.
	/// </summary>
	public ListDataProviderService()
	{
	}

	/// <summary>
	/// Initializes a list-backed provider with an initial set of items.
	/// </summary>
	/// <param name="items">The initial item collection.</param>
	public ListDataProviderService(List<TItem> items)
	{
		List = items;
	}

	/// <summary>
	/// Gets the underlying mutable item list.
	/// </summary>
	public List<TItem> List { get; private set; } = [];

	/// <summary>
	/// Creates a new item at the end of the list.
	/// </summary>
	/// <param name="item">The item to create.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The create operation result.</returns>
	public Task<OperationResponse> CreateAsync(TItem item, CancellationToken cancellationToken)
		=> CreateAsync(item, -1, cancellationToken);

	/// <summary>
	/// Creates a new item at the specified index.
	/// </summary>
	/// <param name="item">The item to create.</param>
	/// <param name="index">Insertion index, or a negative value to append.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The create operation result.</returns>
	public Task<OperationResponse> CreateAsync(TItem item, int index, CancellationToken cancellationToken)
	{
		try
		{
			List.Insert(index < 0 ? List.Count : index, item);
			return Task.FromResult(new OperationResponse { Success = true });
		}
		catch (Exception ex)
		{
			return Task.FromResult(new OperationResponse { ErrorMessage = ex.Message });
		}
	}

	/// <summary>
	/// Deletes an item from the list.
	/// </summary>
	/// <param name="item">The item to delete.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The delete operation result.</returns>
	public Task<OperationResponse> DeleteAsync(TItem item, CancellationToken cancellationToken)
	{
		return List.Remove(item)
			? Task.FromResult(new OperationResponse { Success = true })
			: Task.FromResult(new OperationResponse { ErrorMessage = "Not found" });
	}

	/// <summary>
	/// Returns all items currently in the list.
	/// </summary>
	/// <param name="request">The data request context.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A response containing all items and total count.</returns>
	public Task<DataResponse<TItem>> GetDataAsync(DataRequest<TItem> request, CancellationToken cancellationToken)
		=> Task.FromResult(new DataResponse<TItem>(List, List.Count));

	/// <summary>
	/// Applies a property delta to an existing item.
	/// </summary>
	/// <param name="item">The item to update.</param>
	/// <param name="delta">Property values keyed by property name.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The update operation result.</returns>
	public Task<OperationResponse> UpdateAsync(TItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
		try
		{
			if (item is not null)
			{
				var idx = List.IndexOf(item);
				if (idx == -1)
				{
					return Task.FromResult(new OperationResponse { ErrorMessage = "Not found" });
				}

				var itemType = item.GetType();
				foreach (var name in delta.Keys)
				{
					var propInfo = itemType.GetProperty(name);
					if (propInfo is null)
					{
						return Task.FromResult(new OperationResponse { ErrorMessage = $"Property {name} not found" });
					}

					if (!propInfo.CanWrite)
					{
						return Task.FromResult(new OperationResponse { ErrorMessage = $"Property {name} can not be written too" });
					}

					propInfo.SetValue(item, delta[name]);
				}
			}

			return Task.FromResult(new OperationResponse { Success = true });
		}
		catch (Exception ex)
		{
			return Task.FromResult(new OperationResponse { ErrorMessage = ex.Message });
		}
	}
}
