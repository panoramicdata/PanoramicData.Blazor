using System;

namespace PanoramicData.Blazor
{
	public class FileExplorerItem
	{
		private string? _parentPath;

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
		/// Gets or sets the absolute path to the item.
		/// </summary>
		public string Path { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the full path of the parent item.
		/// </summary>
		/// <remarks>If left unset will return the Directory from the Path property.</remarks>
		public string ParentPath
		{
			get
			{
				return _parentPath == null ? System.IO.Path.GetDirectoryName(Path) ?? string.Empty : _parentPath;
			}
			set
			{
				_parentPath = value;
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
				var name = System.IO.Path.GetFileName(Path);
				return string.IsNullOrWhiteSpace(name) ? Path : name;
			}
		}

		/// <summary>
		/// Gets or sets the size in bytes of the item.
		/// </summary>
		public long FileSize { get; set; }

		/// <summary>
		/// Gets or sets the date and time the item was created.
		/// </summary>
		public DateTimeOffset DateCreated { get; set; }

		/// <summary>
		/// Gets or sets the date and time the item was last modified.
		/// </summary>
		public DateTimeOffset DateModified { get; set; }

		/// <summary>
		/// Returns the Name property of this item.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Gets the file extension.
		/// </summary>
		public string FileExtension
			=> string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(Path)) ? "" : System.IO.Path.GetExtension(Path).Substring(1);
	}
}
