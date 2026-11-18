using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests of <see cref="ChatProgressDisplayMode.Ticker"/>, which shows a reply's latest progress step on one line.
/// </summary>
public partial class PDMessageTests
{
	private IRenderedComponent<PDMessage> RenderTicker(ChatMessage message)
		=> RenderMessage(message, p => p.Add(x => x.ProgressDisplayMode, ChatProgressDisplayMode.Ticker));

	private static ChatMessage Typing(params string[] steps)
	{
		var message = Message(false, MessageType.Typing, "ignored while typing");
		message.ProgressSteps = steps;
		return message;
	}

	private static List<(string Text, bool IsLeaving, string? AriaHidden)> TickerSteps(IRenderedComponent<PDMessage> component)
		=> [.. component.FindAll(".pdchat-progress-ticker-line .pdchat-progress-ticker-step")
			.Select(step => (step.TextContent, step.ClassList.Contains("leaving"), step.GetAttribute("aria-hidden")))];

	/// <summary>Verifies that a message shows every step as a list unless the ticker is asked for.</summary>
	[Fact]
	public void ProgressSteps_AreAListByDefault()
	{
		var component = RenderMessage(Typing("Searching", "Reading"));

		component.FindAll(".pdchat-progress-steps li").Select(li => li.TextContent).Should().Equal("Searching", "Reading");
		component.FindAll(".pdchat-progress-ticker, .pdchat-progress-all").Should().BeEmpty();
		component.FindAll(".typing-ellipsis").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that the ticker shows the latest step, the one before it rolling out hidden from screen readers, the
	/// clock and every step on request, in place of the list and the typing dots.
	/// </summary>
	[Fact]
	public void Ticker_ShowsTheLatestStep_AndTheOneRollingOut()
	{
		var component = RenderTicker(Typing("Searching", "Reading", "Writing"));

		TickerSteps(component).Should().Equal(("Reading", true, "true"), ("Writing", false, null));
		component.Find(".pdchat-progress-ticker-line").GetAttribute("role").Should().Be("status");
		component.Find(".pdchat-progress-elapsed").GetAttribute("aria-hidden").Should().Be("true");
		component.Find(".pdchat-progress-all summary").TextContent.Should().Be("3 steps so far");
		component.FindAll(".pdchat-progress-all li").Select(li => li.TextContent).Should().Equal("Searching", "Reading", "Writing");
		component.FindAll(".pdchat-progress-steps, .typing-ellipsis").Should().BeEmpty();
		component.Markup.Should().NotContain("ignored while typing");
	}

	/// <summary>Verifies that a ticker with one step shows it, with nothing rolling out and no list to open.</summary>
	[Fact]
	public void Ticker_WithOneStep_ShowsItAlone()
	{
		var component = RenderTicker(Typing("Searching"));

		TickerSteps(component).Should().Equal(("Searching", false, null));
		component.FindAll(".pdchat-progress-all").Should().BeEmpty();
	}

	/// <summary>Verifies that a ticker with no steps yet shows the pulse and the clock, and nothing else.</summary>
	[Fact]
	public void Ticker_BeforeAnyStep_ShowsThePulseAndTheClock()
	{
		var message = Typing();
		message.ProgressSteps = null;

		var component = RenderTicker(message);

		TickerSteps(component).Should().BeEmpty();
		component.FindAll(".pdchat-progress-ticker-pulse, .pdchat-progress-elapsed").Should().HaveCount(2);
		component.FindAll(".pdchat-progress-all, .typing-ellipsis").Should().BeEmpty();
	}

	/// <summary>Verifies that a new step takes the line and the step it replaces rolls out.</summary>
	[Fact]
	public void Ticker_OnANewStep_RollsTheLatestOut()
	{
		var message = Typing("Searching", "Reading");
		var component = RenderTicker(message);

		message.ProgressSteps = ["Searching", "Reading", "Writing"];
		component.Render(p => p.Add(x => x.Message, message));

		TickerSteps(component).Should().Equal(("Reading", true, "true"), ("Writing", false, null));
	}

	/// <summary>Verifies that the ticker keeps the reasoning and the partial reply beneath it.</summary>
	[Fact]
	public void Ticker_KeepsTheThoughtsAndThePartialReply()
	{
		var message = Typing("Searching");
		message.Thoughts = [new ChatThought("Plan", "Look it up")];
		message.PartialMessage = "Findings so far";

		var component = RenderTicker(message);

		component.Find(".pdchat-thought summary").TextContent.Should().Be("Plan");
		component.Find(".pdchat-partial").TextContent.Should().Contain("Findings so far");
	}

	/// <summary>Verifies that a finished message shows its text, not a ticker.</summary>
	[Fact]
	public void Ticker_IsNotShownOnceTheAnswerArrives()
	{
		var message = Message(false, text: "The answer");
		message.ProgressSteps = ["Searching"];

		var component = RenderTicker(message);

		component.FindAll(".pdchat-progress, .pdchat-progress-ticker").Should().BeEmpty();
		component.Find(".pdchat-text span").TextContent.Should().Be("The answer");
	}
}
