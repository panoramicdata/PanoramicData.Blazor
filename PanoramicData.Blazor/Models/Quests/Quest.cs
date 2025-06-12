namespace PanoramicData.Blazor.Models.Quests;

public class Quest
{
	public required int Id { get; init; }
	public required string Name { get; init; }
	public required string Description { get; init; }
	public required string ThemeColorHex { get; init; }
}

public class QuestAction
{
	public required int Id { get; init; }
	public required int QuestId { get; init; }
	public required int[] PreviousQuestActionIds { get; init; }
	public required bool IsComplete { get; init; }
	public required string Name { get; init; }
	public required string Description { get; init; }
}

public class PositionedAction
{
	public required QuestAction Action { get; init; }
	public required int LaneIndex { get; init; }
}
