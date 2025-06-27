namespace PanoramicData.Blazor.Services;

public class ListDataProviderService<TItem> : IDataProviderService<TItem>
{

	public ListDataProviderService()
	{
	}

	public ListDataProviderService(List<TItem> items)
	{
		List = items;
	}

	public List<TItem> List { get; private set; } = new();

	public Task<OperationResponse> CreateAsync(TItem item, CancellationToken cancellationToken)
		=> CreateAsync(item, -1, cancellationToken);

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

	public Task<OperationResponse> DeleteAsync(TItem item, CancellationToken cancellationToken)
	{
		return List.Remove(item)
			? Task.FromResult(new OperationResponse { Success = true })
			: Task.FromResult(new OperationResponse { ErrorMessage = "Not found" });
	}

	public Task<DataResponse<TItem>> GetDataAsync(DataRequest<TItem> request, CancellationToken cancellationToken)
		=> Task.FromResult(new DataResponse<TItem>(List, List.Count));

	public Task<OperationResponse> UpdateAsync(TItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
		try
		{
			if (item != null)
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
