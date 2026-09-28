using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.Logging;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDLog"/>: the in-memory log viewer that is also an <see cref="ILogger"/>.
/// </summary>
public class PDLogTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDLogTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static void Write(IRenderedComponent<PDLog> log, LogLevel level, string message, Exception? exception = null)
		=> log.Instance.Log(level, default, message, exception, (state, _) => state);

	/// <summary>
	/// Verifies that a logged message is shown with the icon and timestamp colour for its level.
	/// </summary>
	[Theory]
	[InlineData(LogLevel.Trace, "fas fa-search text-primary", "text-primary")]
	[InlineData(LogLevel.Debug, "fas fa-bug text-secondary", "text-secondary")]
	[InlineData(LogLevel.Information, "fas fa-info-circle text-info", "text-info")]
	[InlineData(LogLevel.Warning, "fas fa-exclamation-triangle text-warning", "text-warning")]
	[InlineData(LogLevel.Error, "fas fa-times-circle text-danger", "text-danger")]
	[InlineData(LogLevel.Critical, "fas fa-bomb text-danger", "text-danger")]
	public void Entry_IsShownWithTheIconAndColourOfItsLevel(LogLevel level, string icon, string timestampClass)
	{
		var log = Render<PDLog>(parameters => parameters.Add(p => p.LogLevel, LogLevel.Trace));

		Write(log, level, "Something happened");

		log.WaitForAssertion(() => log.FindAll(".log-entry").Should().ContainSingle());
		log.Find(".log-entry i").ClassName.Should().Be(icon);
		log.Find(".log-timestamp").ClassList.Should().Contain(timestampClass);
		log.Find(".log-message").TextContent.Should().Be("Something happened");
	}

	/// <summary>
	/// Verifies that entries below the minimum level are dropped, and that <see cref="PDLog.IsEnabled"/> agrees.
	/// </summary>
	[Fact]
	public void EntriesBelowTheMinimumLevel_AreDropped()
	{
		var log = Render<PDLog>(parameters => parameters.Add(p => p.LogLevel, LogLevel.Warning));

		Write(log, LogLevel.Information, "Quiet");
		Write(log, LogLevel.Error, "Loud");

		log.Instance.IsEnabled(LogLevel.Information).Should().BeFalse();
		log.Instance.IsEnabled(LogLevel.Warning).Should().BeTrue();
		log.WaitForAssertion(() => log.FindAll(".log-message").Select(m => m.TextContent).Should().Equal("Loud"));
	}

	/// <summary>
	/// Verifies that a multi-line message is shown one trimmed line per element.
	/// </summary>
	[Fact]
	public void MultiLineMessage_IsShownLineByLine()
	{
		var log = Render<PDLog>();

		Write(log, LogLevel.Information, "first\n  second  \nthird");

		log.WaitForAssertion(() => log.FindAll(".log-message").Select(m => m.TextContent)
			.Should().Equal("first", "second", "third"));
	}

	/// <summary>
	/// Verifies that an exception is shown with its type, message and stack trace.
	/// </summary>
	[Fact]
	public void Exception_IsShownWithTypeMessageAndStack()
	{
		var log = Render<PDLog>();
		Exception thrown;
		try
		{
			throw new InvalidOperationException("It broke");
		}
		catch (InvalidOperationException ex)
		{
			thrown = ex;
		}

		Write(log, LogLevel.Error, "Failure", thrown);

		log.WaitForAssertion(() => log.Find(".log-exception-type").TextContent.Should().Be(typeof(InvalidOperationException).FullName));
		log.Find(".log-exception-message").TextContent.Should().Be("It broke");
		log.Find(".log-exception-stack").TextContent.Should().Contain(nameof(Exception_IsShownWithTypeMessageAndStack));
	}

	/// <summary>
	/// Verifies that an exception that was never thrown has no stack trace to show.
	/// </summary>
	[Fact]
	public void UnthrownException_HasNoStackTrace()
	{
		var log = Render<PDLog>();

		Write(log, LogLevel.Error, "Failure", new InvalidOperationException("Never thrown"));

		log.WaitForAssertion(() => log.Find(".log-exception-message").TextContent.Should().Be("Never thrown"));
		log.FindAll(".log-exception-stack").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the timestamp, icon and exception can each be turned off.
	/// </summary>
	[Fact]
	public void TimestampIconAndException_CanBeHidden()
	{
		var log = Render<PDLog>(parameters => parameters
			.Add(p => p.ShowTimestamp, false)
			.Add(p => p.ShowIcon, false)
			.Add(p => p.ShowException, false));

		Write(log, LogLevel.Error, "Failure", new InvalidOperationException("Hidden"));

		log.WaitForAssertion(() => log.FindAll(".log-entry").Should().ContainSingle());
		log.FindAll(".log-timestamp").Should().BeEmpty();
		log.FindAll(".log-entry i").Should().BeEmpty();
		log.FindAll(".log-exception-message").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the container height follows the row count, and wrapping follows the word wrap setting.
	/// </summary>
	[Theory]
	[InlineData(true, "normal")]
	[InlineData(false, "nowrap")]
	public void Layout_FollowsRowsAndWordWrap(bool wordWrap, string whiteSpace)
	{
		var log = Render<PDLog>(parameters => parameters
			.Add(p => p.Rows, 4)
			.Add(p => p.WordWrap, wordWrap)
			.Add(p => p.CssClass, "my-log"));

		Write(log, LogLevel.Information, "Entry");

		var container = log.Find(".log-container");
		container.ClassList.Should().Contain("my-log");
		container.GetAttribute("style").Should().Contain("height: 10em");
		log.WaitForAssertion(() => log.Find(".log-entry").GetAttribute("style").Should().Contain($"white-space: {whiteSpace}"));
	}

	/// <summary>
	/// Verifies that the timestamp uses the configured format, in UTC unless local time is asked for.
	/// </summary>
	[Theory]
	[InlineData(false, "Z")]
	[InlineData(true, null)]
	public void Timestamp_UsesTheFormatAndTimeZone(bool useLocalTime, string? expected)
	{
		var log = Render<PDLog>(parameters => parameters
			.Add(p => p.UtcTimestampFormat, "%K")
			.Add(p => p.UseLocalTime, useLocalTime));

		Write(log, LogLevel.Information, "Entry");

		var expectedText = expected ?? DateTime.Now.ToString("%K", System.Globalization.CultureInfo.InvariantCulture);
		log.WaitForAssertion(() => log.Find(".log-timestamp").TextContent.Should().Be(expectedText));
	}

	/// <summary>
	/// Verifies that once the capacity is reached the oldest entry makes way for the newest.
	/// </summary>
	[Fact]
	public void Capacity_DropsTheOldestEntry()
	{
		var log = Render<PDLog>(parameters => parameters.Add(p => p.Capacity, 2));

		Write(log, LogLevel.Information, "one");
		Write(log, LogLevel.Information, "two");
		Write(log, LogLevel.Information, "three");

		log.WaitForAssertion(() => log.FindAll(".log-message").Select(m => m.TextContent).Should().Equal("two", "three"));
	}

	/// <summary>
	/// Verifies that lowering the capacity trims the oldest entries, and that a capacity below one is treated as one.
	/// </summary>
	[Fact]
	public void LoweringTheCapacity_TrimsTheOldestEntries()
	{
		var log = Render<PDLog>();
		Write(log, LogLevel.Information, "one");
		Write(log, LogLevel.Information, "two");
		Write(log, LogLevel.Information, "three");

		log.Render(parameters => parameters.Add(p => p.Capacity, 0));

		log.Instance.Capacity.Should().Be(1);
		log.WaitForAssertion(() => log.FindAll(".log-message").Select(m => m.TextContent).Should().Equal("three"));
	}

	/// <summary>
	/// Verifies that reverse order shows the newest entry first.
	/// </summary>
	[Fact]
	public void Reverse_ShowsTheNewestEntryFirst()
	{
		var log = Render<PDLog>(parameters => parameters.Add(p => p.Reverse, true));

		Write(log, LogLevel.Information, "older");
		var marker = DateTime.UtcNow;
		SpinWait.SpinUntil(() => DateTime.UtcNow > marker);
		Write(log, LogLevel.Information, "newer");

		log.WaitForAssertion(() => log.FindAll(".log-message").Select(m => m.TextContent).Should().Equal("newer", "older"));
	}

	/// <summary>
	/// Verifies that clearing removes every entry.
	/// </summary>
	[Fact]
	public async Task Clear_RemovesEveryEntry()
	{
		var log = Render<PDLog>();
		Write(log, LogLevel.Information, "one");
		Write(log, LogLevel.Information, "two");

		await log.InvokeAsync(log.Instance.Clear);

		log.FindAll(".log-entry").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that scopes are not supported, so beginning one returns nothing to dispose.
	/// </summary>
	[Fact]
	public void BeginScope_ReturnsNull()
	{
		var log = Render<PDLog>();

		log.Instance.BeginScope("scope").Should().BeNull();
	}

	/// <summary>
	/// Verifies that with tailing on, each new entry asks the page to scroll the log to the bottom.
	/// </summary>
	[Fact]
	public void Tail_ScrollsToTheBottomOnEachEntry()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.SetupVoid("scrollToBottom", _ => true).SetVoidResult();
		var log = Render<PDLog>(parameters => parameters.Add(p => p.Tail, true));

		Write(log, LogLevel.Information, "one");
		Write(log, LogLevel.Information, "two");

		log.WaitForAssertion(() => module.Invocations["scrollToBottom"].Should().HaveCount(2));
	}

	/// <summary>
	/// Verifies that without tailing no scroll is requested.
	/// </summary>
	[Fact]
	public void WithoutTail_NoScrollIsRequested()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		var log = Render<PDLog>();

		Write(log, LogLevel.Information, "one");

		log.WaitForAssertion(() => log.FindAll(".log-entry").Should().ContainSingle());
		module.Invocations["scrollToBottom"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that tailing is harmless when the scripting module could not be loaded.
	/// </summary>
	[Fact]
	public void Tail_WithoutTheModule_StillLogs()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var log = Render<PDLog>(parameters => parameters.Add(p => p.Tail, true));

		Write(log, LogLevel.Information, "one");

		log.WaitForAssertion(() => log.FindAll(".log-message").Select(m => m.TextContent).Should().Equal("one"));
	}
}
