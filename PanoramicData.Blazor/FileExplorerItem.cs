using System;

namespace PanoramicData.Blazor
{
	public class FileExplorerItem
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
				// check GetDirectoryName output as returns black path separator in path and
				// will return \ for \folder and tree component expects null or empty string
				// to indicate no parent (root item)
				var parentPath = System.IO.Path.GetDirectoryName(Path);
				if(!string.IsNullOrWhiteSpace(parentPath))
				{
					// force all paths to use forward slashes as separators
					parentPath = parentPath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
				}
				return parentPath == "/" ? string.Empty : parentPath;
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
			set
			{
				var name = System.IO.Path.GetFileName(Path);
				if (string.IsNullOrWhiteSpace(name))
				{
					Path = value;
				}
				else
				{

					Path = $"{ParentPath}/{value}";
				}
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
			return Name;
		}

		/// <summary>
		/// Gets the file extension.
		/// </summary>
		public string FileExtension
			=> string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(Path)) ? "" : System.IO.Path.GetExtension(Path).Substring(1);
	}
}
