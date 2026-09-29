using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="PDStudioOptions"/>.</summary>
public class PDStudioOptionsTests
{
	/// <summary>A new set of options describes a light HTML studio with every panel shown and a 30 second timeout.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var options = new PDStudioOptions();

		options.Language.Should().Be("html");
		options.Theme.Should().Be("light");
		options.MainSplitSizes.Should().Equal(75.0, 25.0);
		options.TopSplitSizes.Should().Equal(50.0, 50.0);
		options.IsLoggingVisible.Should().BeTrue();
		options.DefaultLogLevel.Should().Be(LogLevel.Debug);
		options.ShowMenu.Should().BeTrue();
		options.ShowToolbar.Should().BeTrue();
		options.ShowStatusBar.Should().BeTrue();
		options.IsEditingEnabledDuringExecution.Should().BeTrue();
		options.ExecutionTimeoutSeconds.Should().Be(30);
		options.CustomProperties.Should().BeEmpty();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var options = new PDStudioOptions
		{
			Language = "sql",
			Theme = "dark",
			MainSplitSizes = [60, 40],
			TopSplitSizes = [30, 70],
			IsLoggingVisible = false,
			DefaultLogLevel = LogLevel.Warning,
			ShowMenu = false,
			ShowToolbar = false,
			ShowStatusBar = false,
			IsEditingEnabledDuringExecution = false,
			ExecutionTimeoutSeconds = 5,
			CustomProperties = new Dictionary<string, object> { ["x"] = 1 }
		};

		options.Language.Should().Be("sql");
		options.Theme.Should().Be("dark");
		options.MainSplitSizes.Should().Equal(60, 40);
		options.TopSplitSizes.Should().Equal(30, 70);
		options.IsLoggingVisible.Should().BeFalse();
		options.DefaultLogLevel.Should().Be(LogLevel.Warning);
		options.ShowMenu.Should().BeFalse();
		options.ShowToolbar.Should().BeFalse();
		options.ShowStatusBar.Should().BeFalse();
		options.IsEditingEnabledDuringExecution.Should().BeFalse();
		options.ExecutionTimeoutSeconds.Should().Be(5);
		options.CustomProperties.Should().ContainKey("x");
	}
}
