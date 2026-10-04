namespace PanoramicData.Blazor.Demo.Data;

/// <summary>
/// Tree traversal and path helpers for <see cref="DirectoryEntry"/>.
/// </summary>
public partial class DirectoryEntry
{
	public int Count() => Reduce((_, pv) => pv + 1, 0);

	public void ForEach(Action<DirectoryEntry> action)
	{
		action(this);
		foreach (var item in Items)
		{
			item.ForEach(action);
		}
	}

	public string Path() => Path("/");

	public string Path(string separator)
	{
		var stack = new Stack<string>();
		var node = this;
		while (node != null)
		{
			if (!string.IsNullOrWhiteSpace(node.Name))
			{
				stack.Push(node.Name);
			}

			node = node.Parent;
		}

		var path = string.Join(separator, [.. stack]);
		return $"{separator}{path}";
	}

	public T Reduce<T>(Func<DirectoryEntry, T, T> func, T previousValue)
	{
		var value = func(this, previousValue);
		foreach (var item in Items)
		{
			value = item.Reduce(func, value);
		}

		return value;
	}

	public override string ToString() => Path();

	public IEnumerable<DirectoryEntry> Where(Predicate<DirectoryEntry> predicate)
	{
		var items = new List<DirectoryEntry>();
		ForEach((x) =>
		{
			if (predicate(x))
			{
				items.Add(x);
			}
		});
		return items;
	}
}
