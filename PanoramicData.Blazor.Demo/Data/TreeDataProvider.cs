namespace PanoramicData.Blazor.Demo.Data;

public class TreeDataProvider : IDataProviderService<TreeItem>
{
	private readonly List<TreeItem> _items = new();

	public TreeDataProvider()
	{
		_items.Add(new TreeItem { Id = 2, Name = "Sales", ParentId = null, IsGroup = true, Order = 1 });
		_items.Add(new TreeItem { Id = 11, Name = "Alice", ParentId = 2, Order = 1 });
		_items.Add(new TreeItem { Id = 16, Name = "Fred", ParentId = 2, Order = 2 });
		_items.Add(new TreeItem { Id = 21, Name = "Kevin", ParentId = 2, Order = 3 });
		_items.Add(new TreeItem { Id = 14, Name = "Dave", ParentId = 2, Order = 4 });

		_items.Add(new TreeItem { Id = 3, Name = "Marketing", ParentId = null, IsGroup = true, Order = 2 });
		_items.Add(new TreeItem { Id = 12, Name = "Bob", ParentId = 3, Order = 1 });
		_items.Add(new TreeItem { Id = 13, Name = "Carol", ParentId = 3, Order = 2 });
		_items.Add(new TreeItem { Id = 18, Name = "Harry", ParentId = 3, Order = 3 });

		_items.Add(new TreeItem { Id = 4, Name = "Finance", ParentId = null, IsGroup = true, Order = 3 });
		_items.Add(new TreeItem { Id = 15, Name = "Emma", ParentId = 4, Order = 1 });
		_items.Add(new TreeItem { Id = 17, Name = "Gina", ParentId = 4, Order = 2 });
		_items.Add(new TreeItem { Id = 19, Name = "Ian", ParentId = 4, Order = 3 });
		_items.Add(new TreeItem { Id = 20, Name = "Janet", ParentId = 4, Order = 4 });
	}

	public async Task<DataResponse<TreeItem>> GetDataAsync(DataRequest<TreeItem> request, CancellationToken cancellationToken)
	{
		return await Task.Run(() => new DataResponse<TreeItem>(_items, _items.Count)).ConfigureAwait(false);
	}

	public Task<OperationResponse> CreateAsync(TreeItem item, CancellationToken cancellationToken)
	{
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
		throw new System.NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
	}

	public Task<OperationResponse> DeleteAsync(TreeItem item, CancellationToken cancellationToken)
	{
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
		throw new System.NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
	}

	public Task<OperationResponse> UpdateAsync(TreeItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
		throw new System.NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
	}

	public void ReOrder(TreeItem item, TreeItem target, bool? before)
	{
		if (item.IsGroup && !target.IsGroup)
		{
			return;
		}

		if (item.IsGroup)
		{
			// can only drag group onto other group
			var groups = _items.Where(x => x.IsGroup && x.Id > 0).OrderBy(x => x.Order).ToList();
			var s = groups.Find(x => x.Id == item.Id);
			if (s != null)
			{
				groups.Remove(s);
				var tIdx = groups.IndexOf(target);
				groups.Insert(before == true ? tIdx : tIdx + 1, s);
				for (var i = 0; i < groups.Count; i++)
				{
					groups[i].Order = i + 1;
				}
			}
		}
		else
		{
			// drag person onto another person or group

		}
	}
}
