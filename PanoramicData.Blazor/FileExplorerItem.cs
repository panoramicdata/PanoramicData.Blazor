using System;

namespace PanoramicData.Blazor
{
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
		/// Gets or sets whether this item has the ReadOnly attribute set.
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
		/// Gets or sets whether this item can be moved or copied.
		/// </summary>
		public bool CanCopyMove { get; set; } = true;

		/// <summary>
		/// Gets or sets the upload progress.
		/// </summary>
		public double UploadProgress { get; set; }

		/// <summary>
		/// Gets or sets the absolute path to the item.
		/// </summary>
		public string Path { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the full path of the parent item.
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
				return (idx == 0) ? "/" : Path.Substring(0, idx);
			}
		}

		/// <summary>
		/// Gets or sets the type of item.
		/// </summary>
		public FileExplorerItemType EntryType { get; set; }

		/// <summary>
		/// Gets the name of the item.
		/// </summary>
		public string Name
		{
			get
			{
				//   /                         -> "/"
				//   /abc.txt                  => "abc.txt"
				//   /folder1/abc.txt          => "abc.txt"
				//   /folder1/folder2/abc.txt  => "abc.txt"
				if (Path == "/")
				{
					return "/";
				}
				return Path.Substring(Path.LastIndexOf('/') + 1);
			}
			set
			{
				// item is being renamed -> adjust entire path
				Path = $"{ParentPath}/{value}";
			}
		}

		/// <summary>
		/// Gets or sets the size in bytes of the item.
		/// </summary>
		public long FileSize { get; set; }

		/// <summary>
		/// Gets or sets the date and time the item was created.
		/// </summary>
		public DateTimeOffset? DateCreated { get; set; }

		/// <summary>
		/// Gets or sets the date and time the item was last modified.
		/// </summary>
		public DateTimeOffset? DateModified { get; set; }

		/// <summary>
		/// Returns the Name property of this item.
		/// </summary>
		public override string ToString()
		{
			return Path;
		}

		public int CompareTo(object obj)
		{
			FileExplorerItem item = (FileExplorerItem)obj;
			return Name.CompareTo(item.Name);
		}

		/// <summary>
		/// Gets the file extension.
		/// </summary>
		public string FileExtension
			=> string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(Path)) ? "" : System.IO.Path.GetExtension(Path).Substring(1);
	}
}
