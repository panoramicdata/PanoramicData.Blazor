namespace PanoramicData.Blazor
{
	/// <summary>
	/// The ConfirmFileOperationArgs class provides arguments for a File Operation event callback.
	/// </summary>
	public class FileOperationArgs
	{
		/// <summary>
		/// Initializes a new instance of the ConfirmFileOperationArgs class.
		/// </summary>
		/// <param name="operation">Name of the operation being requested</param>
		/// <param name="items">Items to be affected by the operation</param>
		public FileOperationArgs(string operation, FileExplorerItem[] items)
		{
			Operation = operation;
			Items = items;
		}

		/// <summary>
		/// Gets the name of the operation being requested.
		/// </summary>
		public string Operation { get; }

		/// <summary>
		/// Gets the file items to be affected by the operation.
		/// </summary>
		public FileExplorerItem[] Items { get; }

		/// <summary>
		/// Gets or sets whether the operation should be canceled.
		/// </summary>
		public bool Cancel { get; set; }
	}
}
