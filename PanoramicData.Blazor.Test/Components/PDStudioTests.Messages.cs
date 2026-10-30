using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The Messages tab: what it records, filtering, clearing, and keeping messages while hidden.
/// </summary>
public partial class PDStudioTests
{
	private async Task LogAsync(IRenderedComponent<PDStudio> studio, LogLevel level, string text)
		=> await studio.InvokeAsync(() => _service.Raise(new StudioExecutionEventArgs
		{
			EventType = StudioExecutionEventType.Log,
			Status = text,
			LogLevel = level
		}));

	/// <summary>
	/// Verifies that a service message holding braces is shown as written rather than failing as a format string.
	/// </summary>
	[Fact]
	public async Task Messages_TextWithBraces_IsShownAsWritten()
	{
		var studio = RenderStudio();

		await LogAsync(studio, LogLevel.Warning, "Unexpected '{' near {0} in SELECT");

		studio.WaitForAssertion(() => LogMessages(studio).Should().Equal("Unexpected '{' near {0} in SELECT"));
	}

	/// <summary>
	/// Verifies that the level filter and the search box narrow the list, and the tab's badge counts errors.
	/// </summary>
	[Fact]
	public async Task Messages_FilterByLevelAndText()
	{
		var studio = RenderStudio();
		await LogAsync(studio, LogLevel.Information, "Connected");
		await LogAsync(studio, LogLevel.Warning, "No LIMIT given");
		await LogAsync(studio, LogLevel.Error, "Relation missing");
		await LogAsync(studio, LogLevel.Error, "Timed out");

		studio.Find(".pd-studio-messages-level").Change(LogLevel.Warning.ToString());
		LogMessages(studio).Should().Equal("No LIMIT given", "Relation missing", "Timed out");

		studio.Find(".pd-studio-messages-search").Input("time");
		LogMessages(studio).Should().Equal("Timed out");

		studio.Find(".pd-studio-tab-badge").TextContent.Should().Be("2 errors");
	}

	/// <summary>
	/// Verifies that Clear removes every message.
	/// </summary>
	[Fact]
	public async Task Messages_Clear_RemovesThemAll()
	{
		var studio = RenderStudio();
		await LogAsync(studio, LogLevel.Information, "Connected");

		studio.Find(".pd-studio-messages-clear").Click();

		studio.Instance.Messages.Should().BeEmpty();
		LogMessages(studio).Should().BeEmpty();
		studio.FindAll(".pd-studio-tab-badge").Should().BeEmpty();
		studio.Find(".pd-studio-messages-empty").TextContent.Should().Be("No messages");
	}

	/// <summary>
	/// Verifies that Clear on an empty list does nothing, and that messages after a clear start a fresh list.
	/// </summary>
	[Fact]
	public async Task Messages_AfterClear_StartAFreshList()
	{
		var studio = RenderStudio();

		studio.Find(".pd-studio-messages-clear").Click();
		studio.Instance.Messages.Should().BeEmpty();

		await LogAsync(studio, LogLevel.Information, "Before");
		studio.Find(".pd-studio-messages-clear").Click();
		await LogAsync(studio, LogLevel.Warning, "After");

		LogMessages(studio).Should().Equal("After");
		studio.FindAll(".pd-studio-messages-empty").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a filter matching nothing says so, unlike an empty list, and clearing it brings them back.
	/// </summary>
	[Fact]
	public async Task Messages_FilterMatchingNothing_SaysSo()
	{
		var studio = RenderStudio();
		await LogAsync(studio, LogLevel.Information, "First run");
		await LogAsync(studio, LogLevel.Information, "Second run");

		studio.Find(".pd-studio-messages-search").Input("RUN");
		LogMessages(studio).Should().Equal("First run", "Second run");

		studio.Find(".pd-studio-messages-search").Input("nowhere");
		LogMessages(studio).Should().BeEmpty();
		studio.Find(".pd-studio-messages-empty").TextContent.Should().Be("No messages match the filter");

		studio.Find(".pd-studio-messages-search").Input(string.Empty);
		LogMessages(studio).Should().Equal("First run", "Second run");
	}

	/// <summary>
	/// Verifies that a successful run is styled as a success, apart from plain information and from errors.
	/// </summary>
	[Fact]
	public async Task Messages_SuccessInformationAndErrors_AreStyledApart()
	{
		_service.ResultSet = Devices();
		var studio = RenderStudio();
		await LogAsync(studio, LogLevel.Information, "Connected");
		await TypeAsync(studio, "SELECT * FROM devices");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());
		await LogAsync(studio, LogLevel.Error, "Relation missing");

		studio.WaitForAssertion(() => studio.FindAll(".pd-studio-message").Should().HaveCount(3));
		var styles = studio.FindAll(".pd-studio-message").Select(m => (
			Css: m.ClassList.First(c => c.StartsWith("level-", StringComparison.Ordinal)),
			Icon: m.QuerySelector("i")!.ClassName,
			Level: m.QuerySelector(".pd-studio-message-level")!.TextContent));
		styles.Should().Equal(
			("level-information", "fas fa-info-circle", "Info"),
			("level-success", "fas fa-check-circle", "Success"),
			("level-error", "fas fa-times-circle", "Error"));
		studio.Instance.Messages[1].IsSuccess.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that messages recorded while the tab is hidden are there once it is shown again.
	/// </summary>
	[Fact]
	public async Task Messages_AreKeptWhileTheTabIsHidden()
	{
		var options = new PDStudioOptions { IsLoggingVisible = false };
		var studio = RenderStudio(options);

		await LogAsync(studio, LogLevel.Information, "Recorded while hidden");
		studio.FindAll(".pd-studio-messages").Should().BeEmpty();

		options.IsLoggingVisible = true;
		studio.Render(p => p.Add(x => x.Options, options));

		LogMessages(studio).Should().Equal("Recorded while hidden");
	}

	/// <summary>
	/// Verifies that the tabs switch which panel is shown, and a hidden Messages tab cannot stay selected.
	/// </summary>
	[Fact]
	public void Tabs_SwitchThePanel()
	{
		var options = new PDStudioOptions();
		var studio = RenderStudio(options);

		studio.FindAll(".pd-studio-tab")[1].Click();
		studio.Find(".pd-studio-messages").HasAttribute("hidden").Should().BeFalse();
		studio.Find(".pd-studio-tab.active").TextContent.Should().Contain("Messages");

		options.IsLoggingVisible = false;
		studio.Render(p => p.Add(x => x.Options, options));

		studio.Find(".pd-studio-tab.active").TextContent.Trim().Should().Be("Results");
	}
}
