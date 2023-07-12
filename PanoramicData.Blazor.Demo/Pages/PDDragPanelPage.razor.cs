namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDragPanelPage
{
	private readonly List<Job> _jobs = new();
	private readonly List<Job> _jobs2 = new();

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	protected override void OnInitialized()
	{
		// create items - physically order by description
		_jobs.AddRange(new[]
		{
			new Job { Id = 1, Description = "Job A" },
			new Job { Id = 2, Description = "Job Z" },
			new Job { Id = 3, Description = "Job C" },
			new Job { Id = 4, Description = "Job B" },
			new Job { Id = 5, Description = "Job X" }
		}.OrderBy(x => x.Description));

		_jobs2.AddRange(new[]
		{
			new Job { Id = 4, Description = "Job 4" },
			new Job { Id = 1, Description = "Job 1" },
			new Job { Id = 5, Description = "Job 5" },
			new Job { Id = 3, Description = "Job 3" },
			new Job { Id = 2, Description = "Job 2" }
		});
	}


	public void OnItemOrderChanged(DragOrderChangeArgs<Job> args)
	{
		// commit the re-order - in this case order is physical order
		_jobs.Clear();
		_jobs.AddRange(args.Items);

		// log event
		var jobString = string.Join(", ", args.Items.Select(x => x.Description).ToArray());
		EventManager?.Add(new Event("ItemOrderChanged", new EventArgument("Item", jobString)));
	}
}

public class Job
{
	public int Id { get; set; }

	public bool Selected { get; set; }
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