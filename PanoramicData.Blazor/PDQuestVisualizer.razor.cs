using PanoramicData.Blazor.Models.Quests;

namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that renders an SVG-based visualization of quests and their actions.
/// </summary>
public partial class PDQuestVisualizer
{
	/// <summary>
	/// Gets or sets the list of quests to be visualized.
	/// </summary>
	[Parameter]
	public List<Quest> Quests { get; set; } = [];

	/// <summary>
	/// Gets or sets the list of quest actions to be visualized.
	/// </summary>
	[Parameter]
	public List<QuestAction> QuestActions { get; set; } = [];

	/// <summary>
	/// Gets or sets the height of each quest lane.
	/// </summary>
	[Parameter]
	public int QuestHeight { get; set; } = 120;

	/// <summary>
	/// Gets or sets the margin between each quest lane.
	/// </summary>
	[Parameter]
	public int QuestMargin { get; set; } = 10;

	/// <summary>
	/// Gets or sets the radius of the quest action nodes.
	/// </summary>
	[Parameter]
	public int QuestActionRadius { get; set; } = 20;

	/// <summary>
	/// Gets or sets the positioned quest actions computed during rendering.
	/// </summary>
	protected List<PositionedAction> PositionedActions { get; set; } = [];

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		PositionedActions = AssignLanes();
	}

	/// <summary>
	/// Gets the X coordinate for the given quest action in the SVG layout.
	/// </summary>
	/// <param name="action">The quest action to calculate the position for.</param>
	/// <returns>The X pixel coordinate.</returns>
	protected int GetX(QuestAction action)
	{
		var index = QuestActions
			.Where(a => a.QuestId == action.QuestId)
			.ToList()
			.IndexOf(action);
		return 100 + index * 130;
	}

	/// <summary>
	/// Gets the Y coordinate for the given quest ID in the SVG layout.
	/// </summary>
	/// <param name="questId">The quest identifier.</param>
	/// <returns>The Y pixel coordinate for the quest lane.</returns>
	protected int GetQuestY(int questId)
		=> questId * (QuestHeight + QuestMargin);

	private List<PositionedAction> AssignLanes()
	{
		var positioned = new List<PositionedAction>();
		var usedLanes = new Dictionary<int, HashSet<int>>(); // QuestId -> Used lanes

		var sorted = TopoSort(QuestActions);
		foreach (var action in sorted)
		{
			var questId = action.QuestId;
			if (!usedLanes.TryGetValue(questId, out HashSet<int>? value))
			{
				value = [];
				usedLanes[questId] = value;
			}

			int lane = 0;
			while (value.Contains(lane))
			{
				lane++;
			}

			value.Add(lane);

			positioned.Add(new PositionedAction { Action = action, LaneIndex = lane });
		}

		return positioned;
	}

	private static List<QuestAction> TopoSort(List<QuestAction> actions)
	{
		var result = new List<QuestAction>();
		var visited = new HashSet<int>();
		var map = actions.ToDictionary(a => a.Id);

		void Visit(QuestAction a)
		{
			if (visited.Contains(a.Id))
			{
				return;
			}

			foreach (var pid in a.PreviousQuestActionIds ?? [])
			{
				if (map.TryGetValue(pid, out var p))
				{
					Visit(p);
				}
			}

			visited.Add(a.Id);
			result.Add(a);
		}

		foreach (var a in actions)
		{
			Visit(a);
		}

		return result;
	}

	/// <summary>
	/// Gets the total SVG height based on the number of quests.
	/// </summary>
	protected string SvgHeight => $"{((QuestHeight + QuestMargin) * Quests.Count)}px";
}
