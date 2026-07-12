namespace PanoramicData.Blazor.Models;

/// <summary>
/// A simple implementation of <see cref="IDragItem"/> suitable for use in drag-and-drop demos and tests.
/// </summary>
public class SimpleDragItem : IDragItem
{
	/// <inheritdoc />
	public bool CanDrag { get; set; } = true;

	/// <inheritdoc />
	public string Id { get; set; } = Guid.NewGuid().ToString();

	/// <summary>Gets or sets the display text for this drag item.</summary>
	public string Text { get; set; } = string.Empty;

	/// <inheritdoc />
	public override string ToString()
	{
		return Text;
	}
}

/// <summary>
/// Represents a work item used in drag-and-drop demos.
/// </summary>
public class JobModel
{
	/// <summary>Gets or sets the unique identifier of the job.</summary>
	public int Id { get; set; }
	/// <summary>Gets or sets the current status of the job.</summary>
	public JobStatuses Status { get; set; }
	/// <summary>Gets or sets a description of the job.</summary>
	public string Description { get; set; } = string.Empty;
	/// <summary>Gets or sets the date and time the job was last updated.</summary>
	public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Specifies the lifecycle status of a <see cref="JobModel"/>.
/// </summary>
public enum JobStatuses
{
	/// <summary>The job has not been started.</summary>
	Todo,
	/// <summary>The job is in progress.</summary>
	Started,
	/// <summary>The job has been completed.</summary>
	Completed
}