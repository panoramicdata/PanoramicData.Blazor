using System;

namespace PanoramicData.Blazor
{
	public class FileExplorerItem
	{
		//private string? _parentPath;

		/// <summary>
		/// Gets or sets the absolute path to the item.
		/// </summary>
		public string Path { get; set; } = string.Empty;

		public string ParentPath
		{
			get
			{
				//return _parentPath == null ? System.IO.Path.GetDirectoryName(Path) ?? string.Empty : _parentPath;
				return System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
			}
			//set
			//{
			//	_parentPath = value;
			//}
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
	}

	public enum FileSystemEntryTypes
	{
		Directory,
		File
	}
}
