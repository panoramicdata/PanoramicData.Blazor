namespace PanoramicData.Blazor.Demo.Data;

public partial class DirectoryEntry
{
	public string Alias { get; set; } = string.Empty;
	public bool CanAddItems => !IsReadOnly;
	public bool CanCopyMove { get; set; } = true;
	public bool CanDelete { get; set; } = true;
	public bool CanRemoveItems => !IsReadOnly;
	public bool CanRename { get; set; } = true;
	public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset DateModified { get; set; } = DateTimeOffset.UtcNow;
	public bool IsHidden { get; set; }
	public bool IsSystem { get; set; }
	public bool IsReadOnly { get; set; }
	public List<DirectoryEntry> Items { get; } = [];
	public string Name { get; set; } = string.Empty;
	public DirectoryEntry? Parent { get; set; }
	public long Size { get; set; }
	public FileExplorerItemType Type { get; set; }

	public DirectoryEntry()
	{
	}

	public DirectoryEntry(string name, params DirectoryEntry[] items)
		: this(name, false, true, true, items)
	{
	}

	public DirectoryEntry(string name, bool readOnly, bool canDelete, bool canRename, params DirectoryEntry[] items)
	{
		Name = name;
		CanDelete = canDelete;
		CanRename = canRename;
		IsReadOnly = readOnly;
		AddItems(items);
	}

	public DirectoryEntry(string name, FileExplorerItemType type, int size)
		: this(name, type, size, false)
	{
	}

	public DirectoryEntry(string name, FileExplorerItemType type, int size, bool readOnly)
		: this(name, type, size, readOnly, true, true)
	{
	}

	public DirectoryEntry(string name, FileExplorerItemType type, int size, bool readOnly, bool canDelete, bool canRename)
	{
		Name = name;
		Type = type;
		Size = size;
		CanDelete = canDelete;
		CanRename = canRename;
		IsReadOnly = readOnly;
	}

	public DirectoryEntry(params DirectoryEntry[] items)
		: this(string.Empty, items)
	{
	}

	private void AddItems(IEnumerable<DirectoryEntry> items)
	{
		foreach (var item in items)
		{
			item.Parent = this;
			Items.Add(item);
		}
	}

	public DirectoryEntry Clone() => Clone(true);

	public DirectoryEntry Clone(bool deep)
	{
		var clone = new DirectoryEntry
		{
			CanCopyMove = CanCopyMove,
			CanDelete = CanDelete,
			CanRename = CanRename,
			DateCreated = DateCreated,
			DateModified = DateModified,
			IsHidden = IsHidden,
			IsReadOnly = IsReadOnly,
			IsSystem = IsSystem,
			Name = Name,
			Size = Size,
			Type = Type
		};
		if (deep)
		{
			clone.AddItems(Items.Select(x => x.Clone(true)));
		}

		return clone;
	}

	public FileExplorerItem ToFileExploreritem() => ToFileExploreritem("/");

	public FileExplorerItem ToFileExploreritem(string pathSeparator) => new()
	{
		CanCopyMove = CanCopyMove,
		CanDelete = CanDelete,
		CanRename = CanRename,
		DateCreated = DateCreated,
		DateModified = DateModified,
		EntryType = Type,
		FileSize = Size,
		HasSubFolders = Items.Any(x => x.Type == FileExplorerItemType.Directory),
		IsHidden = IsHidden,
		IsReadOnly = IsReadOnly,
		IsSystem = IsSystem,
		Name = string.IsNullOrWhiteSpace(Alias) ? Name : Alias,
		Path = Path(pathSeparator)
	};
}
