namespace PanoramicData.Blazor.Services;

/// <summary>
/// In-memory data provider for card-deck scenarios.
/// </summary>
/// <typeparam name="TItem">The card item type.</typeparam>
public class CardDeckDataProviderService<TItem> : IDataProviderService<TItem>
	where TItem : class
{
	/// <summary>
	/// Initializes an empty card-deck data provider.
	/// </summary>
	public CardDeckDataProviderService()
	{
	}

	/// <summary>
	/// Gets the underlying mutable item list.
	/// </summary>
	public List<TItem> List { get; private set; } = [];

	/// <summary>
	/// Adds an item to the provider.
	/// </summary>
	/// <param name="item">The item to create.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The create operation result.</returns>
	public Task<OperationResponse> CreateAsync(TItem item, CancellationToken cancellationToken)
	{
		List.Add(item);
		return Task.FromResult(new OperationResponse { Success = true });
	}

	/// <summary>
	/// Removes an item from the provider.
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
	/// Returns data based on the provided request and optional response filter.
	/// </summary>
	/// <param name="request">The data request containing filters and transforms.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A data response containing filtered results and total item count.</returns>
	public Task<DataResponse<TItem>> GetDataAsync(DataRequest<TItem> request, CancellationToken cancellationToken)
	{
		var operation = request.ResponseFilter;

		var results = (operation is not null)
			? [.. operation(List)]
			: List;

		return Task.FromResult(new DataResponse<TItem>(results, List.Count));
	}

	/// <summary>
	/// Returns data for the given request after a delay.
	/// </summary>
	/// <param name="request">The data request containing filters and transforms.</param>
	/// <param name="delay">Delay in seconds before executing the request.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A data response containing filtered results and total item count.</returns>
	public async Task<DataResponse<TItem>> GetDataAsync(DataRequest<TItem> request, double delay, CancellationToken cancellationToken)
	{
		await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

		return await GetDataAsync(request, cancellationToken);
	}

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
				var index = List.IndexOf(item);

				// Cannot be found
				if (index == -1)
				{
					return Task.FromResult(new OperationResponse { ErrorMessage = "Not found" });
				}

				// Apply changes to item
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
