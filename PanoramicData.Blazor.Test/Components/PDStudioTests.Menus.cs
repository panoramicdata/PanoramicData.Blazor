using AwesomeAssertions;
using BlazorMonaco.Editor;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Menu and editor tests for <see cref="PDStudio"/>.
/// </summary>
public partial class PDStudioTests
{
	/// <summary>
	/// Verifies that the examples on the File menu load code suited to the studio's language.
	/// </summary>
	[Theory]
	[InlineData("sql", "LoadExample1", "SELECT * FROM customers")]
	[InlineData("mssql", "LoadExample2", "GROUP BY c.id")]
	[InlineData("ncalc", "LoadExample1", "Basic NCalc Expression Examples")]
	[InlineData("ncalc", "LoadExample2", "Test Error Scenarios")]
	[InlineData("html", "LoadExample1", "<h1>Hello World</h1>")]
	[InlineData("html", "LoadExample2", "Sample Table")]
	[InlineData("javascript", "LoadExample1", "console.log('Hello World!')")]
	[InlineData("javascript", "LoadExample2", "function fibonacci")]
	[InlineData("python", "LoadExample1", "Example 1 for python")]
	[InlineData("python", "LoadExample2", "Example 2 for python")]
	public async Task FileMenu_ExamplesSuitTheLanguage(string language, string example, string expected)
	{
		var studio = RenderStudio(new PDStudioOptions { Language = language });

		await MenuAsync(studio, "File", example);
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		studio.WaitForAssertion(() => _service.Calls.Should().ContainSingle().Which.Code.Should().Contain(expected));
	}

	/// <summary>
	/// Verifies that New clears the code and results, so there is then nothing to execute.
	/// </summary>
	[Fact]
	public async Task FileMenu_New_ClearsTheCode()
	{
		var studio = RenderStudio();
		await TypeAsync(studio, "<p>Hi</p>");

		await MenuAsync(studio, "File", "New");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		_service.Calls.Should().BeEmpty();
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("New document created"));
		studio.FindAll(".pd-studio-results-placeholder").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that Open and Save, not yet implemented, only log that they were chosen, and unknown keys do nothing.
	/// </summary>
	[Fact]
	public async Task FileMenu_OpenAndSave_OnlyLog()
	{
		var studio = RenderStudio();

		await MenuAsync(studio, "File", "Open");
		await MenuAsync(studio, "File", "Save");
		await MenuAsync(studio, "File", "Unknown");

		studio.WaitForAssertion(() => LogMessages(studio).Should()
			.Equal("Open clicked (not implemented)", "Save clicked (not implemented)"));
	}

	/// <summary>
	/// Verifies that the Logging menu sets the studio's default log level, treating unknown keys as Information.
	/// </summary>
	[Theory]
	[InlineData("Debug", LogLevel.Debug)]
	[InlineData("Info", LogLevel.Information)]
	[InlineData("Warning", LogLevel.Warning)]
	[InlineData("Error", LogLevel.Error)]
	[InlineData("Verbose", LogLevel.Information)]
	public async Task LoggingMenu_SetsTheLogLevel(string key, LogLevel expected)
	{
		var options = new PDStudioOptions { DefaultLogLevel = LogLevel.Critical };
		var studio = RenderStudio(options);

		await MenuAsync(studio, "Logging", key);

		options.DefaultLogLevel.Should().Be(expected);
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain($"Log level changed to {expected}"));
	}

	/// <summary>
	/// Verifies that the editor is configured with the studio's defaults, then the caller's own adjustments.
	/// </summary>
	[Fact]
	public void EditorOptions_ApplyTheDefaultsThenTheCallersAdjustments()
	{
		var studio = RenderStudio(more: p => p
			.Add(x => x.InitializeMonacoOptions, o => o.WordWrap = "off"));
		var options = new StandaloneEditorConstructionOptions();

		studio.FindComponent<PDMonacoEditor>().Instance.InitializeOptions!(options);

		options.AutomaticLayout.Should().BeTrue();
		options.ScrollBeyondLastLine.Should().BeFalse();
		options.Minimap!.Enabled.Should().BeFalse();
		options.Folding.Should().BeTrue();
		options.WordWrap.Should().Be("off");
	}

	/// <summary>
	/// Verifies that the editor is given the studio's language and theme.
	/// </summary>
	[Fact]
	public void Editor_UsesTheLanguageAndTheme()
	{
		var studio = RenderStudio(new PDStudioOptions { Language = "sql", Theme = "vs-dark" });

		var editor = studio.FindComponent<PDMonacoEditor>().Instance;
		editor.Language.Should().Be("sql");
		editor.Theme.Should().Be("vs-dark");
	}
}
