namespace PanoramicData.Blazor;

/// <summary>
/// A hierarchical multi-select picker: a <see cref="PDTree{TItem}"/> whose nodes each carry a checkbox.
/// Composes the tree via its NodeTemplate, so no tree internals are altered. Checked state is exposed
/// as the set of node keys (<see cref="CheckedKeys"/>), bindable with <c>@bind-CheckedKeys</c>.
/// Typical use: picking multiple folders in one pass (e.g. access-control wizards), where checking a
/// parent implies its subtree and <see cref="IsCheckDisabled"/> can grey out the descendants.
/// </summary>
/// <typeparam name="TItem">The tree item type.</typeparam>
public partial class PDCheckboxTree<TItem> : PDComponentBase where TItem : class
{
	private readonly HashSet<string> _checkedKeys = [];

	/// <summary>
	/// Gets or sets the data provider that supplies the tree items.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public IDataProviderService<TItem> DataProvider { get; set; } = null!;

	/// <summary>
	/// Gets or sets the function that selects the unique key of an item. The string form of this key
	/// is the currency of <see cref="CheckedKeys"/>.
	/// </summary>
	[Parameter] public Func<TItem, object>? KeyField { get; set; }

	/// <summary>
	/// Gets or sets the function that selects the parent key of an item.
	/// </summary>
	[Parameter] public Func<TItem, object>? ParentKeyField { get; set; }

	/// <summary>
	/// Gets or sets the function that selects the display text of an item.
	/// </summary>
	[Parameter] public Func<TItem, object>? TextField { get; set; }

	/// <summary>
	/// Gets or sets the function that determines whether an item is a leaf node.
	/// </summary>
	[Parameter] public Func<TItem, bool>? IsLeaf { get; set; }

	/// <summary>
	/// Gets or sets an optional function returning the icon CSS class for an item. The icon is
	/// rendered between the checkbox and the item text.
	/// </summary>
	[Parameter] public Func<TItem, int, string>? IconCssClass { get; set; }

	/// <summary>
	/// Gets or sets whether child nodes are fetched only when a node is first expanded.
	/// </summary>
	[Parameter] public bool LoadOnDemand { get; set; }

	/// <summary>
	/// Gets or sets whether the root node is displayed.
	/// </summary>
	[Parameter] public bool ShowRoot { get; set; } = true;

	/// <summary>
	/// Gets or sets whether connecting lines are displayed between nodes.
	/// </summary>
	[Parameter] public bool ShowLines { get; set; }

	/// <summary>
	/// Gets or sets an optional comparison used to sort sibling items.
	/// </summary>
	[Parameter] public Comparison<TItem>? Sort { get; set; }

	/// <summary>
	/// Gets or sets an optional callback invoked once the tree has first loaded.
	/// </summary>
	[Parameter] public EventCallback Ready { get; set; }

	/// <summary>
	/// Gets or sets an optional callback invoked when an exception occurs while fetching items.
	/// </summary>
	[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

	/// <summary>
	/// Gets or sets the keys of the currently checked items. Bindable with <c>@bind-CheckedKeys</c>.
	/// </summary>
	[Parameter] public List<string> CheckedKeys { get; set; } = [];

	/// <summary>
	/// Gets or sets the callback invoked whenever the set of checked keys changes.
	/// </summary>
	[Parameter] public EventCallback<List<string>> CheckedKeysChanged { get; set; }

	/// <summary>
	/// Gets or sets an optional function that disables the checkbox of an item.
	/// The item remains visible and expandable; only its checkbox is disabled.
	/// </summary>
	[Parameter] public Func<TItem, bool>? IsCheckDisabled { get; set; }

	/// <summary>
	/// Gets or sets an optional function that marks an item as implicitly checked: its checkbox is
	/// rendered checked and disabled, but its key is NOT part of <see cref="CheckedKeys"/>. Typically
	/// used for descendants of a checked folder, where checking the parent implies the whole subtree.
	/// </summary>
	[Parameter] public Func<TItem, bool>? IsCheckImplied { get; set; }

	/// <summary>
	/// Gets the underlying tree, for advanced operations such as expanding or refreshing nodes.
	/// </summary>
	public PDTree<TItem>? Tree { get; private set; }

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		_checkedKeys.Clear();
		foreach (var key in CheckedKeys ?? [])
		{
			_checkedKeys.Add(key);
		}
	}

	private bool IsChecked(TreeNode<TItem> node)
		=> _checkedKeys.Contains(node.Key) || IsImplied(node);

	private bool IsDisabled(TreeNode<TItem> node)
		=> !IsEnabled
			|| IsImplied(node)
			|| (IsCheckDisabled is not null && node.Data is not null && IsCheckDisabled(node.Data));

	private bool IsImplied(TreeNode<TItem> node)
		=> IsCheckImplied is not null && node.Data is not null && IsCheckImplied(node.Data);

	private async Task OnCheckboxChangedAsync(TreeNode<TItem> node, ChangeEventArgs args)
	{
		if (args.Value is true)
		{
			_checkedKeys.Add(node.Key);
		}
		else
		{
			_checkedKeys.Remove(node.Key);
		}

		CheckedKeys = [.. _checkedKeys];
		await CheckedKeysChanged.InvokeAsync(CheckedKeys).ConfigureAwait(true);
	}
}
