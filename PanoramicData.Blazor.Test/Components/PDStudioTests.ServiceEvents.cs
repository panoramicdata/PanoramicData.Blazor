using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDStudio"/> follows the state and execution events of its studio service.
/// </summary>
public partial class PDStudioTests
{
	private async Task RaiseAsync(IRenderedComponent<PDStudio> studio, StudioExecutionEventType type, string status = "", string output = "")
		=> await studio.InvokeAsync(() => _service.Raise(new StudioExecutionEventArgs
		{
			EventType = type,
			Status = status,
			Output = output,
			LogLevel = LogLevel.Warning
		}));

	/// <summary>
	/// Verifies that the service's started, progress, output and completed events drive the status and results.
	/// </summary>
	[Fact]
	public async Task ServiceEvents_DriveTheStatusAndResults()
	{
		var states = new List<bool>();
		var studio = RenderStudio(more: p => p.Add(x => x.OnExecutionStateChanged, s => states.Add(s)));

		await RaiseAsync(studio, StudioExecutionEventType.Started, "Starting up");
		studio.WaitForAssertion(() => Status(studio).Should().Be("Starting up"));
		PlayButton(studio).TextContent.Should().Contain("Cancel");

		await RaiseAsync(studio, StudioExecutionEventType.Progress, "Halfway");
		studio.WaitForAssertion(() => Status(studio).Should().Be("Halfway"));

		await RaiseAsync(studio, StudioExecutionEventType.UpdateOutput, output: "<b>partial</b>");
		studio.WaitForAssertion(() => studio.Find("iframe").GetAttribute("srcdoc").Should().Contain("<b>partial</b>"));
		Status(studio).Should().Be("Halfway");

		await RaiseAsync(studio, StudioExecutionEventType.UpdateOutput, "Timed out after 5s", "<b>final</b>");
		studio.WaitForAssertion(() => Status(studio).Should().Be("Timed out after 5s"));

		await RaiseAsync(studio, StudioExecutionEventType.OutputComplete, "ignored");
		await RaiseAsync(studio, StudioExecutionEventType.Completed, "done");
		studio.WaitForAssertion(() => Status(studio).Should().Be("Complete"));
		states.Should().Equal(false);
		PlayButton(studio).TextContent.Should().Contain("Execute");
	}

	/// <summary>
	/// Verifies that a cancelled event ends the execution with a cancelled status.
	/// </summary>
	[Fact]
	public async Task ServiceEvents_Cancelled_EndsTheExecution()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.OnExecutionStateChanged, _ => { }));

		await RaiseAsync(studio, StudioExecutionEventType.Started, "Starting");
		await RaiseAsync(studio, StudioExecutionEventType.Cancelled, "by user");

		studio.WaitForAssertion(() => Status(studio).Should().Be("Cancelled"));
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("Execution Cancelled: by user"));
	}

	/// <summary>
	/// Verifies that an error event ends the execution, keeping a timeout message and prefixing any other.
	/// </summary>
	[Theory]
	[InlineData("Timed out after 30s", "Timed out after 30s")]
	[InlineData("Division by zero", "Error: Division by zero")]
	public async Task ServiceEvents_Error_EndsTheExecution(string message, string expected)
	{
		var states = new List<bool>();
		var studio = RenderStudio(more: p => p.Add(x => x.OnExecutionStateChanged, s => states.Add(s)));

		await RaiseAsync(studio, StudioExecutionEventType.Started, "Starting");
		await RaiseAsync(studio, StudioExecutionEventType.Error, message);

		studio.WaitForAssertion(() => Status(studio).Should().Be(expected));
		states.Should().Equal(false);
		LogMessages(studio).Should().Contain($"Execution error: {message}");
	}

	/// <summary>
	/// Verifies that a log event from the service is written to the log panel.
	/// </summary>
	[Fact]
	public async Task ServiceEvents_Log_IsWrittenToTheLog()
	{
		var studio = RenderStudio();

		await RaiseAsync(studio, StudioExecutionEventType.Log, "Parsed 3 tokens");

		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("Parsed 3 tokens"));
		studio.Find(".log-entry i").ClassList.Should().Contain("text-warning");
	}
}
