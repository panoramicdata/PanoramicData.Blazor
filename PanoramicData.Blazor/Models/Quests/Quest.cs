namespace PanoramicData.Blazor.Models.Quests;

/// <summary>
/// Represents a named quest (workflow objective) with a theme color.
/// </summary>
public class Quest
{
	/// <summary>Gets the unique identifier for this quest.</summary>
	public required int Id { get; init; }
	/// <summary>Gets the display name of the quest.</summary>
	public required string Name { get; init; }
	/// <summary>Gets a human-readable description of the quest's goal.</summary>
	public required string Description { get; init; }
	/// <summary>Gets the hex color string (e.g. <c>#FF5733</c>) used to theme this quest.</summary>
	public required string ThemeColorHex { get; init; }
}

/// <summary>
/// Represents a single action (step) within a <see cref="Quest"/>, including its dependencies and completion state.
/// </summary>
public class QuestAction
{
	/// <summary>Gets the unique identifier for this action.</summary>
	public required int Id { get; init; }
	/// <summary>Gets the identifier of the <see cref="Quest"/> this action belongs to.</summary>
	public required int QuestId { get; init; }
	/// <summary>Gets the identifiers of actions that must be completed before this action becomes available.</summary>
	public required int[] PreviousQuestActionIds { get; init; }
	/// <summary>Gets whether this action has been completed.</summary>
	public required bool IsComplete { get; init; }
	/// <summary>Gets the display name of this action.</summary>
	public required string Name { get; init; }
	/// <summary>Gets a human-readable description of what this action involves.</summary>
	public required string Description { get; init; }
}

/// <summary>
/// Associates a <see cref="QuestAction"/> with a display lane index for layout purposes.
/// </summary>
public class PositionedAction
{
	/// <summary>Gets the quest action being positioned.</summary>
	public required QuestAction Action { get; init; }
	/// <summary>Gets the zero-based index of the display lane this action is rendered in.</summary>
	public required int LaneIndex { get; init; }
}
