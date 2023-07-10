namespace PanoramicData.Blazor.Models;

public class SimpleDragItem : IDragItem
{
	public bool CanDrag { get; set; } = true;

	public string Id { get; set; } = Guid.NewGuid().ToString();

	public string Text { get; set; } = string.Empty;

	public override string ToString()
	{
		return Text;
	}
}



public class JobModel
{
	public int Id { get; set; }
	public JobStatuses Status { get; set; }
	public string Description { get; set; } = string.Empty;
	public DateTime LastUpdated { get; set; }
}

public enum JobStatuses
{
	Todo,
	Started,
	Completed
}