namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDDragPanelPage
{
	private List<Job> _jobs = new();

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

	protected override void OnInitialized()
	{
		// create items - physically order by description
		_jobs = new()
		{
			new ("a", "Job A"),
			new ("b", "Job B"),
			new ("c", "Job C"),
			new ("x", "Job X"),
			new ("z", "Job Z")
		};
	}


	public void OnItemOrderChanged(DragOrderChangeArgs<Job> args)
	{
		// commit the re-order - in this case order is physical order
		_jobs.Clear();
		_jobs.AddRange(args.Items);

		// log event
		var jobString = string.Join(", ", args.Items.Select(x => x.Text).ToArray());
		EventManager?.Add(new Event("ItemOrderChanged", new EventArgument("New Order", jobString)));
	}

	public void OnSelectionChanged(IEnumerable<Job> selection)
	{
		// log event
		var jobString = string.Join(", ", selection.Select(x => x.Text).ToArray());
		EventManager?.Add(new Event("SelectionChanged", new EventArgument("Selection", jobString)));
	}
}

public class Job : SelectableItem
{
	public Job(string id)
		: base(id)
	{
	}

	public Job(string id, string text)
		: base(id, text)
	{
	}

	public Job.Statuses Status { get; set; }

	public DateTime LastUpdated { get; set; }

	public enum Statuses
	{
		Todo,
		Started,
		Completed
	}
}