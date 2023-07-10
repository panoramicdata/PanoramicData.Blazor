namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDragPanelPage
{
	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	private List<JobModel> _jobs = new()
	{
		new JobModel { Id = 1, Description = "Job 1", Status = JobStatuses.Todo },
		new JobModel { Id = 2, Description = "Job 2", Status = JobStatuses.Todo },
		new JobModel { Id = 3, Description = "Job 3", Status = JobStatuses.Todo }
	};
	void HandleStatusUpdated(JobModel updatedJob)
	{
		Console.WriteLine(updatedJob.Description);
	}


	private IEnumerable<Job> _items = new[]
	{
		new Job { Id = 1, Description = "Job A" },
		new Job { Id = 2, Description = "Job Z" },
		new Job { Id = 3, Description = "Job C" },
		new Job { Id = 4, Description = "Job B" },
		new Job { Id = 5, Description = "Job X" }
	}.OrderBy(x => x.Description);

	public void OnItemReOrdered(Job job)
	{
		EventManager?.Add(new Event("ItemReOrdered", new EventArgument("Item", job)));
	}
}

public class Job
{
	public int Id { get; set; }
	public Job.Statuses Status { get; set; }
	public string Description { get; set; } = string.Empty;
	public DateTime LastUpdated { get; set; }

	public enum Statuses
	{
		Todo,
		Started,
		Completed
	}

	public override string ToString()
	{
		return Description;
	}
}