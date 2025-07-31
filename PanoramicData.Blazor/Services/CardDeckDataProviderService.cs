namespace PanoramicData.Blazor.Services;

public class CardDeckDataProviderService<TItem> : IDataProviderService<TItem>
	where TItem : class
{
	public CardDeckDataProviderService()
	{
	}

	public List<TItem> List { get; private set; } = [];

	public Task<OperationResponse> CreateAsync(TItem item, CancellationToken cancellationToken)
	{
		List.Add(item);
		return Task.FromResult(new OperationResponse { Success = true });
	}

	public Task<OperationResponse> DeleteAsync(TItem item, CancellationToken cancellationToken)
	{
		return List.Remove(item)
		? Task.FromResult(new OperationResponse { Success = true })
		: Task.FromResult(new OperationResponse { ErrorMessage = "Not found" });
	}

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
	/// <param name="request"></param>
	/// <param name="delay"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<DataResponse<TItem>> GetDataAsync(DataRequest<TItem> request, double delay, CancellationToken cancellationToken)
	{
		await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

		return await GetDataAsync(request, cancellationToken);
	}

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
