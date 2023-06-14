namespace PanoramicData.Blazor.Models;

/// <summary>
/// The SortCriteria class provides details of a sort operation.
/// </summary>
public class SortCriteria
{
	/// <summary>
	/// Initializes a new instance of the SortCriteria class.
	/// </summary>
	public SortCriteria()
	{
	}

	/// <summary>
	/// Initializes a new instance of the SortCriteria class.
	/// </summary>
	/// <param name="key">Identifier of the field the sort operation is performed upon</param>
	/// <param name="direction">Direction of the sort.</param>
	public SortCriteria(string key)
	{
		Key = key;
		Direction = SortDirection.Ascending;
	}

	public SortCriteria(string key, SortDirection direction)
	{
		Key = key;
		Direction = direction;
	}

	/// <summary>
	/// Gets or sets the identifier of the field the sort operation is performed upon.
	/// </summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the direction of the sort.
	/// </summary>
	public SortDirection Direction { get; set; }
}
