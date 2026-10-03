namespace PanoramicData.Blazor;

/// <summary>
/// Operations of the <see cref="PDTree{TItem}"/> that walk the whole tree: expanding, collapsing and searching.
/// </summary>
public partial class PDTree<TItem>
{
	/// <summary>
	/// Expands all the branch nodes in the tree.
	/// </summary>
	public void ExpandAll() => RootNode.Walk((n) => { n.IsExpanded = !n.Isleaf; return true; });

	/// <summary>
	/// Expands all the branch nodes in the tree asynchronously.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task ExpandAllAsync() => await RootNode.WalkAsync(async (n) =>
	{
		if (!n.IsExpanded && !n.Isleaf && (ExpandOnExpandAll == null || ExpandOnExpandAll(n)))
		{
			await ToggleNodeIsExpandedAsync(n).ConfigureAwait(true);
		}

		return true;
	}).ConfigureAwait(true);

	/// <summary>
	/// Collapses all the branch nodes in the tree.
	/// </summary>
	public void CollapseAll() => RootNode.Walk((n) => { n.IsExpanded = false; return true; });

	/// <summary>
	/// Searches all nodes until the given criteria is first matched.
	/// </summary>
	/// <param name="predicate">The predicate to match nodes.</param>
	/// <returns>The first matching <see cref="TreeNode{TItem}"/>, or null if not found.</returns>
	public TreeNode<TItem>? Search(Predicate<TreeNode<TItem>> predicate)
	{
		TreeNode<TItem>? found = null;
		RootNode.Walk((n) =>
		{
			if (predicate(n))
			{
				found = n;
				return false;
			}

			return true;
		});
		return found;
	}
}
