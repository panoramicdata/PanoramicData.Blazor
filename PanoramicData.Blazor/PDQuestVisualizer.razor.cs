using PanoramicData.Blazor.Models.Quests;

namespace PanoramicData.Blazor;

public partial class PDQuestVisualizer
{
	[Parameter]
	public List<Quest> Quests { get; set; } = [];

	[Parameter]
	public List<QuestAction> QuestActions { get; set; } = [];

	[Parameter]
	public int QuestHeight { get; set; } = 120;

	[Parameter]
	public int QuestMargin { get; set; } = 10;

	[Parameter]
	public int QuestActionRadius { get; set; } = 20;

	protected List<PositionedAction> PositionedActions = [];

	protected override void OnParametersSet()
	{
		PositionedActions = AssignLanes();
	}

	protected int GetX(QuestAction action)
	{
		var index = QuestActions
			.Where(a => a.QuestId == action.QuestId)
			.ToList()
			.IndexOf(action);
		return 100 + index * 130;
	}

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

	protected string SvgHeight => $"{((QuestHeight + QuestMargin) * Quests.Count)}px";
}
