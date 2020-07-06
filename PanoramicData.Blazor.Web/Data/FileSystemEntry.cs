namespace PanoramicData.Blazor.Web.Data
{
	public class FileSystemEntry
	{
		private string? _parentPath;

		public string Path { get; set; } = string.Empty;

		public string ParentPath
		{
			get
			{
				return string.IsNullOrWhiteSpace(_parentPath) ? System.IO.Path.GetDirectoryName(Path) ?? string.Empty : _parentPath;
			}
			set
			{
				_parentPath = value;
			}
		}

		public FileSystemEntryTypes EntryType { get; set; }

		public string Name
		{
			get
			{
				var name = System.IO.Path.GetFileName(Path);
				return string.IsNullOrWhiteSpace(name) ? Path : name;
			}
		}

		public long FileSize { get; set; }
	}

	public enum FileSystemEntryTypes
	{
		Directory,
		File
	}
}
