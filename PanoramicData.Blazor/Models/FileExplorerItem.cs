namespace PanoramicData.Blazor.Models;

public class FileExplorerItem : IComparable
{
	/// <summary>
	/// Gets or sets whether this item has the Hidden attribute set.
	/// </summary>
	public bool IsHidden { get; set; }

	/// <summary>
	/// Gets or sets whether this item has the System attribute set.
	/// </summary>
	public bool IsSystem { get; set; }

	/// <summary>
	/// Indicates whether a file or a folder can be added to a folder.
	/// </summary>
	public bool IsReadOnly { get; set; }

	/// <summary>
	/// Gets or sets whether this item is currently being uploaded.
	/// </summary>
	public bool IsUploading { get; set; }

	/// <summary>
	/// Gets or sets whether it is known that this node has sub-folders?
	/// </summary>
	public bool? HasSubFolders { get; set; }

	/// <summary>
	/// Gets or sets whether the user may add new files and folders to the folder?
	/// </summary>
	public bool CanAddItems => !IsReadOnly;

	/// <summary>
	/// Gets or sets whether this item can be moved or copied.
	/// </summary>
	public bool CanCopyMove { get; set; } = true;

	/// <summary>
	/// Indicates whether a file or a folder can be deleted.
	/// </summary>
	public bool CanDelete { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the user may remove files and folders from the folder?
	/// </summary>
	public bool CanRemoveItems => !IsReadOnly;

	/// <summary>
	/// Indicates whether a file or a folder can be renamed.
	/// </summary>
	public bool CanRename { get; set; } = true;

	/// <summary>
	/// Gets or sets the upload progress.
	/// </summary>
	public double UploadProgress { get; set; }

	/// <summary>
	/// Gets or sets the absolute path to the item.
	/// </summary>
	public string Path { get; set; } = string.Empty;

	/// <summary>
	/// Gets the full path of the parent item.
	/// </summary>
	public string ParentPath
	{
		get
		{
			//   /                         -> ""
			//   /abc.txt                  => "/"
			//   /folder1/abc.txt          => "/folder1"
			//   /folder1/folder2/abc.txt  => "/folder1/folder2"
			if (Path == "/")
			{
				return "";
			}

			var idx = Path.LastIndexOf('/');
			return (idx == 0) ? "/" : Path[..idx];
		}
	}

	/// <summary>
	/// Gets or sets the type of item.
	/// </summary>
	[Display(Name = "Entry Type")]
	public FileExplorerItemType EntryType { get; set; }

	/// <summary>
	/// Gets the name of the item.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the size in bytes of the item.
	/// </summary>
	[Display(Name = "Size")]
	public long FileSize { get; set; }

	/// <summary>
	/// Gets or sets the date and time the item was created.
	/// </summary>
	[Display(Name = "Created")]
	public DateTimeOffset? DateCreated { get; set; }

	/// <summary>
	/// Gets or sets the date and time the item was last modified.
	/// </summary>
	[Display(Name = "Modified")]
	public DateTimeOffset? DateModified { get; set; }

	/// <summary>
	/// Gets or sets application state attached to this item.
	/// </summary>
	public object? State { get; set; }

	/// <summary>
	/// Renames the item.
	/// </summary>
	/// <param name="name">The new name of the item.</param>
	public void Rename(string name)
	{
		if (Path != "/")
		{
			var idx = Path.LastIndexOf('/') + 1;
			Path = Path[..idx] + name;
			Name = name;
		}
	}

	/// <summary>
	/// Returns the Name property of this item.
	/// </summary>
	public override string ToString() => Path;

	public int CompareTo(object? obj)
	{
		if (obj is null)
		{
			throw new InvalidOperationException();
		}

		FileExplorerItem item = (FileExplorerItem)obj;
		return string.Compare(Name, item?.Name, StringComparison.Ordinal);
	}

	/// <summary>
	/// Gets the file extension.
	/// </summary>
	[Display(Name = "Type")]
	public string FileExtension
		=> string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(Path)) ? "" : System.IO.Path.GetExtension(Path)[1..];

	public FileExplorerItem Clone() => new()
	{
		CanCopyMove = CanCopyMove,
		DateCreated = DateCreated,
		DateModified = DateModified,
		EntryType = EntryType,
		FileSize = FileSize,
		HasSubFolders = HasSubFolders,
		IsHidden = IsHidden,
		IsReadOnly = IsReadOnly,
		IsSystem = IsSystem,
		IsUploading = IsUploading,
		Path = Path,
		UploadProgress = UploadProgress
	};

	public static string GetNameFromPath(string? path)
	{
		if (path == null)
		{
			return string.Empty;
		}

		var parts = path.Split('/');
		return parts.Length > 0 ? parts.Last() : string.Empty;
	}

	/// <summary>
	/// Is the file item name a match with any of the given wild card patterns?
	/// </summary>
	/// <param name="pattern">A search pattern containing * (match 0 or more chars) and ? (match 1 char).</param>
	/// <returns>true if the name is a match, otherwise false.</returns>
	public bool IsNameMatch(string pattern)
	{
		if (string.IsNullOrWhiteSpace(pattern))
		{
			return true;
		}

		// get display filename
		var name = GetNameFromPath(Path);

		// match any provided pattern?
		return pattern
			.Split(';')
			.Any(x => Regex.IsMatch(name, x.Replace("*", ".*").Replace("?", "."), RegexOptions.IgnoreCase));
	}
}
