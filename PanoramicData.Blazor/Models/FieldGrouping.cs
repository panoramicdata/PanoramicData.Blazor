namespace PanoramicData.Blazor.Models;

public class FieldGrouping
{
	/// <summary>
	/// Initializes a new instance of the FieldGrouping class.
	/// </summary>
	/// <param name="groupName">Name of the group the field belongs to.</param>
	public FieldGrouping(string groupName)
	{
		GroupName = groupName;
	}

	/// <summary>
	/// Gets the identifier of the group the field belongs to.
	/// </summary>
	public string GroupName { get; private set; } = string.Empty;
}
