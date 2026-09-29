using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models.Quests;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDQuestVisualizer"/> draws one lane per quest, one node per action placed along
/// its quest's lane, and an arrow from each prerequisite action to the action that depends on it.
/// </summary>
public class PDQuestVisualizerTests : BunitContext
{
	/// <summary>
	/// Verifies that nothing but the shared marker is drawn when there are no quests.
	/// </summary>
	[Fact]
	public void With_no_quests_the_svg_has_no_height_and_no_lanes()
	{
		var component = Render<PDQuestVisualizer>();

		component.Find("svg").GetAttribute("height").Should().Be("0px");
		component.FindAll("g").Should().BeEmpty();
		component.FindAll("marker").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that each quest gets a themed lane, a marker and a heading, and that the SVG height covers
	/// every lane and its margin.
	/// </summary>
	[Fact]
	public void Each_quest_gets_a_themed_lane_and_heading()
	{
		var component = RenderQuests(questHeight: 100, questMargin: 20);

		component.Find("svg").GetAttribute("height").Should().Be("240px");
		component.FindAll("g").Should().HaveCount(2);
		component.Find("#arrowhead_1 polygon").GetAttribute("fill").Should().Be("#ff0000");

		var lanes = component.FindAll("rect");
		lanes[0].GetAttribute("transform").Should().Be("translate(0, 120)");
		lanes[1].GetAttribute("transform").Should().Be("translate(0, 240)");
		component.FindAll("g > text[x='10']").Select(t => t.TextContent).Should().Equal("Alpha", "Beta");
	}

	/// <summary>
	/// Verifies that actions are spaced along their own quest's lane, and that completion is shown by fill
	/// and stroke opacity.
	/// </summary>
	[Fact]
	public void Actions_are_spaced_along_their_lane_and_show_completion()
	{
		var component = RenderQuests(questHeight: 100, questMargin: 20, radius: 10);

		var circles = component.FindAll("circle");
		circles.Should().HaveCount(3);

		// Quest 1 lane starts at 120, so its nodes sit at its centre line, 170.
		circles[0].GetAttribute("transform").Should().Be("translate(100, 170)");
		circles[0].GetAttribute("fill").Should().Be("green");
		circles[0].GetAttribute("stroke-opacity").Should().Be("1.0");
		circles[0].GetAttribute("r").Should().Be("10");
		circles[1].GetAttribute("transform").Should().Be("translate(230, 170)");
		circles[1].GetAttribute("fill").Should().Be("white");
		circles[1].GetAttribute("stroke-opacity").Should().Be("0.3");

		// The only action on quest 2 starts its own lane again.
		circles[2].GetAttribute("transform").Should().Be("translate(100, 290)");
	}

	/// <summary>
	/// Verifies that action names are drawn beneath their nodes.
	/// </summary>
	[Fact]
	public void Action_names_are_drawn_below_their_nodes()
	{
		var component = RenderQuests(questHeight: 100, questMargin: 20, radius: 10);

		var names = component.FindAll("text[text-anchor=middle]");
		names.Select(t => t.TextContent).Should().BeEquivalentTo(["Start", "Next", "Other"]);
		names.Single(t => t.TextContent == "Start").GetAttribute("transform").Should().Be("translate(100, 192)");
	}

	/// <summary>
	/// Verifies that an arrow is drawn from each prerequisite to its dependant, opaque only when both ends
	/// are complete, and that an unknown prerequisite id draws nothing.
	/// </summary>
	[Fact]
	public void Prerequisites_are_joined_by_arrows()
	{
		var component = RenderQuests(questHeight: 100, questMargin: 20, radius: 10);

		var paths = component.FindAll("path");
		paths.Should().HaveCount(2);
		paths.Should().OnlyContain(p => p.GetAttribute("stroke-opacity") == "0.3");

		var withinLane = paths.Single(p => p.GetAttribute("marker-end") == "url(#arrowhead_1)");
		withinLane.GetAttribute("d").Should().Be("M 110 170 C 160 170, 160 170, 210 170");

		var acrossLanes = paths.Single(p => p.GetAttribute("marker-end") == "url(#arrowhead_2)");
		acrossLanes.GetAttribute("d").Should().Be("M 110 170 C 95 170, 95 290, 80 290");
	}

	/// <summary>
	/// Verifies that an arrow between two complete actions is fully opaque.
	/// </summary>
	[Fact]
	public void An_arrow_between_complete_actions_is_opaque()
	{
		var quests = new List<Quest> { CreateQuest(0, "Only", "#00ff00") };
		var actions = new List<QuestAction>
		{
			CreateAction(1, 0, "First", true),
			CreateAction(2, 0, "Second", true, 1)
		};

		var component = Render<PDQuestVisualizer>(parameters => parameters
			.Add(p => p.Quests, quests)
			.Add(p => p.QuestActions, actions));

		component.Find("path").GetAttribute("stroke-opacity").Should().Be("1.0");
	}

	/// <summary>
	/// Verifies that an action listed before its prerequisite is still laid out, as are both of them.
	/// </summary>
	[Fact]
	public void Actions_listed_before_their_prerequisites_are_still_drawn()
	{
		var quests = new List<Quest> { CreateQuest(0, "Only", "#00ff00") };
		var actions = new List<QuestAction>
		{
			CreateAction(2, 0, "Second", false, 1),
			CreateAction(1, 0, "First", true)
		};

		var component = Render<PDQuestVisualizer>(parameters => parameters
			.Add(p => p.Quests, quests)
			.Add(p => p.QuestActions, actions));

		component.FindAll("circle").Should().HaveCount(2);
		component.FindAll("path").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that prerequisites forming a cycle, including an action listing itself, are drawn rather than
	/// recursing forever (issue #189): every action gets one node and every listed prerequisite its arrow,
	/// the edge that closes each cycle being ignored only when ordering the actions.
	/// </summary>
	/// <remarks>
	/// Before the fix this overflowed the stack, which kills the test host rather than failing the test.
	/// The ordering now checks for sufficient stack as it descends, so a regression throws an
	/// <see cref="InsufficientExecutionStackException"/> that fails this test instead.
	/// </remarks>
	[Fact]
	public void Cyclic_prerequisites_are_drawn()
	{
		var quests = new List<Quest> { CreateQuest(0, "Only", "#00ff00") };
		var actions = new List<QuestAction>
		{
			CreateAction(1, 0, "First", false, 2),
			CreateAction(2, 0, "Second", false, 1),
			CreateAction(3, 0, "Self", false, 3)
		};

		var component = Render<PDQuestVisualizer>(parameters => parameters
			.Add(p => p.Quests, quests)
			.Add(p => p.QuestActions, actions));

		component.FindAll("circle").Should().HaveCount(3);
		component.FindAll("text[text-anchor=middle]").Select(t => t.TextContent).Should().BeEquivalentTo(["First", "Second", "Self"]);
		component.FindAll("path").Should().HaveCount(3);
	}

	private IRenderedComponent<PDQuestVisualizer> RenderQuests(int questHeight, int questMargin, int radius = 20)
	{
		var quests = new List<Quest>
		{
			CreateQuest(1, "Alpha", "#ff0000"),
			CreateQuest(2, "Beta", "#0000ff")
		};
		var actions = new List<QuestAction>
		{
			CreateAction(10, 1, "Start", true),
			CreateAction(11, 1, "Next", false, 10, 999),
			CreateAction(20, 2, "Other", false, 10)
		};

		return Render<PDQuestVisualizer>(parameters => parameters
			.Add(p => p.Quests, quests)
			.Add(p => p.QuestActions, actions)
			.Add(p => p.QuestHeight, questHeight)
			.Add(p => p.QuestMargin, questMargin)
			.Add(p => p.QuestActionRadius, radius));
	}

	private static Quest CreateQuest(int id, string name, string colour)
		=> new() { Id = id, Name = name, Description = name, ThemeColorHex = colour };

	private static QuestAction CreateAction(int id, int questId, string name, bool isComplete, params int[] previous)
		=> new()
		{
			Id = id,
			QuestId = questId,
			Name = name,
			Description = name,
			IsComplete = isComplete,
			PreviousQuestActionIds = previous
		};
}
